using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OYA_Web_Application.Models.Services;
using OYA_Web_Application.Services.KeyVault_Service;
using OYA_Web_Application.Services.LDAP_Service;

namespace OYA_Web_Application.Controllers;

public class AuthenticationController : Controller
{
	private readonly ILogger<HomeController> _logger;

	private readonly IConfiguration _configuration;
	private readonly IWebHostEnvironment _webHostEnvironment;

	public const string SessionKeyName = "_Name";

	public AuthenticationController(IConfiguration configuration, ILogger<HomeController> logger, IWebHostEnvironment webHostEnvironment)
	{
		_logger = logger;
		_configuration = configuration;
		_webHostEnvironment = webHostEnvironment;
	}

	public ActionResult Login()
	{
		if (string.Equals(
			base.Request.Query["microsoftSsoError"],
			"unavailable",
			StringComparison.OrdinalIgnoreCase))
		{
			base.ViewBag.MicrosoftSsoError =
				"Microsoft sign-in could not reach or validate the identity service. Check the server's outbound HTTPS access to login.microsoftonline.com, Azure app credentials, and the registered redirect URI, then try again.";
		}

		return View();
	}

	[HttpGet]
	public IActionResult MicrosoftLogin()
	{
		if (string.IsNullOrWhiteSpace(_configuration["AzureAd:TenantId"]) ||
			string.IsNullOrWhiteSpace(_configuration["AzureAd:ClientId"]) ||
			string.IsNullOrWhiteSpace(_configuration["AzureAd:ClientSecret"]))
		{
			ViewBag.MicrosoftSsoError = "Microsoft sign-in is not configured yet. Add AzureAd:TenantId and AzureAd:ClientId in appsettings.json, then set AzureAd:ClientSecret with User Secrets.";
			return View("Login");
		}

		var properties = new AuthenticationProperties
		{
			RedirectUri = Url.Action("Dashboard", "Home") ?? "/"
		};

		return Challenge(properties, "Microsoft");
	}

	[HttpPost]
	public async Task<ActionResult> Login(string username, string password)
	{
		if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
		{
			return View();
		}
		string ldapServer = "";
		string supportUserName;
		string supportPassword;
		if (_webHostEnvironment.IsDevelopment() &&
			base.Request.Host.Host == "localhost" &&
			base.Request.Host.Port == 60474 &&
			HttpContext.Connection.RemoteIpAddress != null &&
			System.Net.IPAddress.IsLoopback(HttpContext.Connection.RemoteIpAddress))
		{
			// Local development login must not depend on Azure Key Vault being reachable.
			supportUserName = "admin@123";
			supportPassword = "admin@123";
		}
		else
		{
			AzureKeyVaultService service = new AzureKeyVaultService(_configuration);
			ldapServer = service.RetrieveSecret("LDAP-Server");
			service.RetrieveSecret("SQL-ConnectionString");
			supportUserName = service.RetrieveSecret("SupportUsername");
			supportPassword = service.RetrieveSecret("SupportPassword");
		}
		new LdapResponse();
		LdapResponse ldapResponse;
		if (username == supportUserName && password == supportPassword)
		{
			ldapResponse = new LdapResponse
			{
				isAuthenticated = true,
				isAuthorizeduser = true,
				cn = "Admin",
				SAMAccountName = "Admin",
				UserPrincipalName = "Admin"
			};
		}
		else
		{
			LdapAuthenticationService ldapService = new LdapAuthenticationService(_configuration);
			ldapResponse = ldapService.ValidateCredentials(ldapServer, username, password);
		}
		if (ldapResponse == null)
		{
			base.ViewBag.TehnicalError = true;
			return View();
		}
		if (ldapResponse != null && ldapResponse.isAuthenticated && ldapResponse.isAuthorizeduser)
		{
			base.HttpContext.Session.SetString("_Name", supportUserName);
			List<Claim> claims = new List<Claim>
			{
				new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name", ldapResponse.cn),
				new Claim("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier", ldapResponse.UserPrincipalName),
				new Claim("SAMAccountName", ldapResponse.SAMAccountName)
			};
			ClaimsIdentity claimsIdentity = new ClaimsIdentity(claims, "Cookies");
			AuthenticationProperties authProperties = new AuthenticationProperties
			{
				IsPersistent = true,
				ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30.0)
			};
			await base.HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(claimsIdentity), authProperties);
			base.HttpContext.Session.SetString("Username", ldapResponse.cn);
			base.ViewBag.isAuthenticated = "Success";
			return RedirectToAction("Dashboard", "Home");
		}
		base.ViewBag.isAuthenticated = ((ldapResponse != null && ldapResponse.isAuthenticated) ? "" : "Error");
		base.ViewBag.isAuthorizeduser = ((ldapResponse != null && ldapResponse.isAuthorizeduser) ? "" : "Error");
		return View();
	}

	public async Task<IActionResult> Logout()
	{
		await base.HttpContext.SignOutAsync("Cookies");
		base.HttpContext.Session.Clear();
		return RedirectToAction("Login", "Authentication");
	}
}
