using System;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace OYA_Web_Application.Services.KeyVault_Service;

public class AzureKeyVaultService
{
	private readonly SecretClient _client;

	public AzureKeyVaultService(IConfiguration configuration)
	{
		var vaultUri = configuration["KeyVault:Uri"]
			?? Environment.GetEnvironmentVariable("OYA_KEY_VAULT_URI")
			?? Environment.GetEnvironmentVariable("AZURE_KEYVAULT_URI")
			?? "https://azrwusprdkv01.vault.azure.net/";

		string? CredentialSetting(string key, string environmentName)
		{
			var configured = configuration[key];
			return !string.IsNullOrWhiteSpace(configured)
				? configured : Environment.GetEnvironmentVariable(environmentName);
		}
		var tenantId = CredentialSetting("KeyVault:TenantId", "AZURE_TENANT_ID");
		var clientId = CredentialSetting("KeyVault:ClientId", "AZURE_CLIENT_ID");
		var clientSecret = CredentialSetting("KeyVault:ClientSecret", "AZURE_CLIENT_SECRET");
		Azure.Core.TokenCredential credential =
			!string.IsNullOrWhiteSpace(tenantId) && !string.IsNullOrWhiteSpace(clientId)
			&& !string.IsNullOrWhiteSpace(clientSecret)
				? new ClientSecretCredential(tenantId, clientId, clientSecret)
				: new DefaultAzureCredential();
		_client = new SecretClient(new Uri(vaultUri), credential);
	}

	public string RetrieveSecret(string secretName)
	{
		try
		{
			return _client.GetSecret(secretName)?.Value?.Value ?? "";
		}
		catch
		{
			return "";
		}
	}
}
