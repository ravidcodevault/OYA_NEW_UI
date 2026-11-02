namespace OYA_Web_Application.Models.Services;

public class LdapResponse
{
	public bool isAuthenticated { get; set; }

	public bool isAuthorizeduser { get; set; }

	public string cn { get; set; }

	public string UserPrincipalName { get; set; }

	public string SAMAccountName { get; set; }
}
