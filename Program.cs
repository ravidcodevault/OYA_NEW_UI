using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using OYA_Web_Application.Hubs;
using OYA_Web_Application.Models.Services;

internal class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Resolve packaged resources relative to the extracted project, not
        // the developer's C: drive or the shell's current directory.
        foreach (var key in new[] { "TemplatePath", "ReportOutputPath", "LogPath" })
        {
            var value = builder.Configuration[key];
            if (string.IsNullOrWhiteSpace(value)) continue;
            var resolved = Path.IsPathRooted(value)
                ? value : Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, value));
            builder.Configuration[key] = key is "TemplatePath" or "ReportOutputPath"
                ? resolved.TrimEnd('\\', '/') + Path.DirectorySeparatorChar : resolved;
        }
        foreach (var entry in builder.Configuration.GetSection("FilePaths").GetChildren())
        {
            if (!string.IsNullOrWhiteSpace(entry.Value) && !Path.IsPathRooted(entry.Value))
                builder.Configuration[entry.Path] = Path.GetFullPath(
                    Path.Combine(builder.Environment.ContentRootPath, entry.Value));
        }
        var reportDirectory = builder.Configuration["ReportOutputPath"];
        if (!string.IsNullOrWhiteSpace(reportDirectory)) Directory.CreateDirectory(reportDirectory);
        var logFile = builder.Configuration["LogPath"];
        if (!string.IsNullOrWhiteSpace(logFile))
            Directory.CreateDirectory(Path.GetDirectoryName(logFile)!);

        // The Windows Event Log provider can throw when the process identity
        // cannot write to the .NET Runtime event source. That logging failure
        // can abort an otherwise handled HTTP request and appear as
        // ERR_EMPTY_RESPONSE in the browser. Keep application diagnostics on
        // stdout so they work for both local runs and IIS stdout capture.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();

        if (builder.Configuration.GetValue<bool>("DataProtection:UseEphemeralProvider"))
        {
            builder.Services.AddDataProtection()
                .UseEphemeralDataProtectionProvider();
        }
        else
        {
            var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
            if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
            {
                Directory.CreateDirectory(dataProtectionKeysPath);
                builder.Services.AddDataProtection()
                    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));
            }
        }

    // =====================================================
    // SIGNALR
    // =====================================================

    builder.Services.AddSignalR();

        // =====================================================
        // CORS
        // =====================================================

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("CORSPolicy", policy =>
            {
                policy.AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials()
                      .SetIsOriginAllowed(_ => true);
            });
        });

        // =====================================================
        // MVC
        // =====================================================

        builder.Services.AddControllersWithViews();

        // =====================================================
        // SESSION
        // =====================================================

        builder.Services.AddDistributedMemoryCache();

        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromMinutes(20);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        // =====================================================
        // AUTHENTICATION
        // =====================================================

        var authenticationBuilder = builder.Services
            .AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
            .AddCookie(options =>
            {
                options.LoginPath = "/Authentication/Login";
                options.ExpireTimeSpan = TimeSpan.FromMinutes(20);
            });

        var azureAd = builder.Configuration.GetSection("AzureAd");
        var microsoftSsoConfigured =
            !string.IsNullOrWhiteSpace(azureAd["TenantId"]) &&
            !string.IsNullOrWhiteSpace(azureAd["ClientId"]) &&
            !string.IsNullOrWhiteSpace(azureAd["ClientSecret"]);

        if (microsoftSsoConfigured)
        {
            authenticationBuilder.AddOpenIdConnect("Microsoft", options =>
            {
                var tenantId = azureAd["TenantId"];
                var instance = azureAd["Instance"] ?? "https://login.microsoftonline.com/";

                options.Authority = $"{instance.TrimEnd('/')}/{(string.IsNullOrWhiteSpace(tenantId) ? "common" : tenantId)}/v2.0";
                options.ClientId = azureAd["ClientId"] ?? "";
                options.ClientSecret = azureAd["ClientSecret"] ?? "";
                options.CallbackPath = azureAd["CallbackPath"] ?? "/signin-oidc";
                options.ResponseType = "code";
                options.SaveTokens = false;
                options.GetClaimsFromUserInfoEndpoint = true;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
                options.TokenValidationParameters.NameClaimType = "name";
                options.Events = new OpenIdConnectEvents
                {
                    OnTokenValidated = context =>
                    {
                        var username = context.Principal?.FindFirst("preferred_username")?.Value
                            ?? context.Principal?.FindFirst("email")?.Value
                            ?? context.Principal?.Identity?.Name;

                        if (!string.IsNullOrWhiteSpace(username))
                        {
                            context.HttpContext.Session.SetString("_Name", username);
                            context.HttpContext.Session.SetString("Username", username);
                        }

                        return Task.CompletedTask;
                    }
                };
            });
        }

        // =====================================================
        // HTTP CONTEXT ACCESSOR
        // =====================================================

        builder.Services.TryAddSingleton<
            IHttpContextAccessor,
            HttpContextAccessor>();

        // =====================================================
        // DATABASE ENTRY
        // =====================================================

        builder.Services.AddScoped<DatabaseEntryRepository>();
        if (builder.Configuration.GetValue("TemplateSync:Enabled", true))
        {
            builder.Services.AddSingleton<DailyTemplateDistributorSyncService>();
            builder.Services.AddHostedService(
                services => services.GetRequiredService<DailyTemplateDistributorSyncService>());
        }

        // =====================================================
        // BUILD APPLICATION
        // =====================================================

        var app = builder.Build();

        // =====================================================
        // ERROR HANDLING
        // =====================================================

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Home/Error");
        }

        // OpenID Connect performs metadata and token calls as part of the
        // challenge/callback pipeline, after the controller action has run.
        // Catch failures for these two endpoints so a network/TLS failure
        // returns the user to login instead of a dropped connection.
        app.Use(async (context, next) =>
        {
            try
            {
                await next();
            }
            catch (Exception ex) when (
                !context.Response.HasStarted &&
                (context.Request.Path.StartsWithSegments(
                     "/Authentication/MicrosoftLogin",
                     StringComparison.OrdinalIgnoreCase) ||
                 context.Request.Path.StartsWithSegments(
                     "/signin-oidc",
                     StringComparison.OrdinalIgnoreCase)))
            {
                app.Logger.LogError(
                    ex,
                    "Microsoft sign-in failed while contacting or validating the identity provider.");
                context.Response.Redirect(
                    "/Authentication/Login?microsoftSsoError=unavailable");
            }
        });

        // =====================================================
        // STATIC FILES
        // =====================================================

        app.UseStaticFiles();

        // =====================================================
        // ROUTING
        // =====================================================

        app.UseRouting();

        // =====================================================
        // CORS
        // =====================================================

        app.UseCors("CORSPolicy");

        // =====================================================
        // SESSION
        // =====================================================

        app.UseSession();

        // =====================================================
        // AUTHENTICATION
        // =====================================================

        app.UseAuthentication();

        // =====================================================
        // AUTHORIZATION
        // =====================================================

        app.UseAuthorization();

        // =====================================================
        // SIGNALR
        // =====================================================

        app.MapHub<MessageHub>("/messagehub");

        // =====================================================
        // MVC ROUTE
        // =====================================================

        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Dashboard}/{id?}");

        // =====================================================
        // RUN
        // =====================================================

        app.Run();
    }

}
