using System;
using System.DirectoryServices.Protocols;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using OYA_Web_Application.Models.Services;
using OYA_Web_Application.Services.KeyVault_Service;

namespace OYA_Web_Application.Services.LDAP_Service;

public class LdapAuthenticationService
{
	private readonly IConfiguration _configuration;

	public LdapAuthenticationService(IConfiguration configuration)
	{
		_configuration = configuration;
	}

	public LdapResponse ValidateCredentials(string ldapServer, string ldapUsername, string ldapPassword)
	{
		int ldapPort = 389;
		try
		{
			using LdapConnection connection = new LdapConnection(ldapServer + ":" + ldapPort);
			connection.SessionOptions.ProtocolVersion = 3;
			connection.AuthType = AuthType.Basic;
			connection.Credential = new NetworkCredential(ldapUsername, ldapPassword);
			connection.Bind();
			return GetUsernameFromAD(connection, ldapUsername);
		}
		catch (LdapException ex)
		{
			Console.WriteLine("Authentication failed: " + ex.Message);
			return new LdapResponse();
		}
		catch (Exception)
		{
			return new LdapResponse();
		}
	}

	private LdapResponse GetUsernameFromAD(LdapConnection ldapConnection, string emailAddress)
	{
		try
		{
			string containerDn = "DC=heliasllc,DC=local";
			string user = "test user";
			SearchRequest searchUser = new SearchRequest(containerDn, "(&(objectClass=user)(userPrincipalName=" + emailAddress + "))", SearchScope.Subtree, (string[])null);
			SearchResponse searchResponseUser = (SearchResponse)ldapConnection.SendRequest(searchUser);
			if (searchResponseUser.Entries.Count == 1)
			{
				SearchResultEntry userEntry = searchResponseUser.Entries[0];
				string cn = userEntry.Attributes["cn"]?[0]?.ToString();
				string userPrincipalName = userEntry.Attributes["userPrincipalName"]?[0]?.ToString();
				string sAMAccountName = userEntry.Attributes["sAMAccountName"]?[0]?.ToString();
				DirectoryAttribute memberOf = userEntry.Attributes["memberOf"];
				AzureKeyVaultService service = new AzureKeyVaultService(_configuration);
				string groupName = service.RetrieveSecret("ADGroup-name");
				bool isAuthorizeduser = false;
				if (memberOf != null)
				{
					foreach (byte[] group in memberOf)
					{
						string groupDn = Encoding.UTF8.GetString(group);
						Console.WriteLine(groupDn);
						Console.WriteLine(group.ToString());
						if (groupDn.ToString().Contains(groupName))
						{
							isAuthorizeduser = true;
						}
					}
				}
				return new LdapResponse
				{
					isAuthenticated = true,
					isAuthorizeduser = isAuthorizeduser,
					cn = cn,
					UserPrincipalName = userPrincipalName,
					SAMAccountName = sAMAccountName
				};
			}
			return new LdapResponse();
		}
		catch (Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
			return new LdapResponse();
		}
	}
}
