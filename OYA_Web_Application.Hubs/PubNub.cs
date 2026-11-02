using PubnubApi;

namespace OYA_Web_Application.Hubs;

public class PubNub
{
    private readonly IConfiguration _configuration;
    private const string UserIdValue = "OYA_WEB_USER";
    private const string ChannelName = "OYA_SYNC_UPDATE";

    public PubNub(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendMessageAsync(
        Dictionary<string, string> message,
        CancellationToken cancellationToken = default)
    {
        var publishKey = _configuration["PubNub:PublishKey"]
            ?? throw new InvalidOperationException("Configure PubNub keys in appsettings.json.");
        var subscribeKey = _configuration["PubNub:SubscribeKey"]
            ?? throw new InvalidOperationException("Configure PubNub keys in appsettings.json.");
        var secretKey = _configuration["PubNub:SecretKey"];
        var token = _configuration["PubNub:AuthToken"];

        if (string.IsNullOrWhiteSpace(token) && string.IsNullOrWhiteSpace(secretKey))
        {
            throw new InvalidOperationException(
                "PubNub authorization is not configured. Set PubNub:SecretKey in server-side secrets, " +
                "or supply a valid PubNub:AuthToken with write permission on OYA_SYNC_UPDATE for OYA_WEB_USER.");
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            // Issue a token only on the server; restrict it to one channel and user.
            var issuer = new Pubnub(new PNConfiguration(new UserId(UserIdValue))
            {
                PublishKey = publishKey,
                SubscribeKey = subscribeKey,
                SecretKey = secretKey
            });
            try
            {
                var grant = await issuer.GrantToken()
                    .TTL(5)
                    .AuthorizedUuid(UserIdValue)
                    .Resources(new PNTokenResources
                    {
                        Channels = new Dictionary<string, PNTokenAuthValues>
                        {
                            [ChannelName] = new PNTokenAuthValues { Write = true }
                        }
                    })
                    .ExecuteAsync()
                    .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

                if (grant.Status.Error)
                {
                    var detail = grant.Status.ErrorData?.Information ?? "No transport details";
                    foreach (var key in new[] { publishKey, subscribeKey, secretKey })
                        if (!string.IsNullOrWhiteSpace(key)) detail = detail.Replace(key, "[redacted]");
                    Console.WriteLine("PubNub grant failure: " + detail);
                }

                if (grant.Status.Error || string.IsNullOrWhiteSpace(grant.Result?.Token))
                {
                    throw new InvalidOperationException(
                        "PubNub could not authorize the sync publisher. " +
                    $"HTTP {grant.Status.StatusCode}, category {grant.Status.Category}. " +
                    "Verify the server-side SecretKey belongs to this keyset and Access Manager is configured.");
                }
                token = grant.Result.Token;
            }
            finally
            {
                issuer.Destroy();
            }
        }

        var publisher = new Pubnub(new PNConfiguration(new UserId(UserIdValue))
        {
            PublishKey = publishKey,
            SubscribeKey = subscribeKey
        });
        try
        {
            publisher.SetAuthToken(token);
            var publish = await publisher.Publish()
                .Channel(ChannelName)
                .Message(message)
                .ExecuteAsync()
                .WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);

            if (publish.Status.Error)
            {
                throw new InvalidOperationException(
                    "PubNub rejected the sync command. Check the token's expiry, user, and write permission on OYA_SYNC_UPDATE.");
            }
            Console.WriteLine("PubNub accepted the sync command. Timetoken: " + publish.Result.Timetoken);
        }
        finally
        {
            publisher.Destroy();
        }
    }
}
