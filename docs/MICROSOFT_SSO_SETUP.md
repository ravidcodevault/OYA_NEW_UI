# Microsoft sign-in setup

## 1. Register the application in Microsoft Entra ID

Create an app registration for OYA and choose the account type that matches your organization. For a single organization, use accounts in this organizational directory only.

Under **Authentication**, add a **Web** redirect URI for each local URL you use:

- `https://localhost:60474/signin-oidc`
- `http://localhost:60475/signin-oidc`
- `http://localhost:60474/signin-oidc` (if running with the local command used for this project)

Add the production callback URI too when you deploy, using your real HTTPS domain. The callback path is `/signin-oidc` unless you change `AzureAd:CallbackPath`.

Copy `appsettings.example.json` to `appsettings.json`, then replace `YOUR_TENANT_ID` and `YOUR_CLIENT_ID` with the **Directory (tenant) ID** and **Application (client) ID** from the app registration overview:

```json
"AzureAd": {
  "Instance": "https://login.microsoftonline.com/",
  "TenantId": "YOUR_TENANT_ID",
  "ClientId": "YOUR_CLIENT_ID",
  "ClientSecret": "YOUR_CLIENT_SECRET_VALUE",
  "CallbackPath": "/signin-oidc"
}
```

Put the client secret **Value** from the Entra app registration in `ClientSecret`. The real `appsettings.json` is excluded from Git, so your local secret won’t be committed. The tracked example contains placeholders only. You can alternatively keep the secret in .NET User Secrets:

Create a client secret under **Certificates & secrets**. Set it with .NET User Secrets from the project directory:

```sh
dotnet user-secrets set "AzureAd:ClientSecret" "YOUR_CLIENT_SECRET"
```

The project has a `UserSecretsId`, so this stores it in your local .NET secrets store. In production, provide `AzureAd__ClientSecret` through your hosting platform's secret/environment-variable settings. Restart the app after changing these settings so the OpenID Connect handler picks them up.

## 3. Run and sign in

Run the app, open the login page, and choose **Sign in with Microsoft**. Microsoft returns to `/signin-oidc`, then the app redirects to the dashboard.

The tenant ID restricts sign-in to the tenant you selected. The application still needs its existing authorization policy if you want to limit which users inside that tenant may access OYA.
