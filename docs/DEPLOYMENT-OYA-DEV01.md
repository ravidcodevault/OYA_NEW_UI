# Deployment to OYA-DEV01

Only pushes to main (or manual Actions runs on main) deploy this repository. Build runs on a GitHub-hosted Windows runner. Deployment uses the repository-specific runner OYA-DEV01-OYA-NEW-UI, with label oya-dev01-new-ui. The deployment script also checks the computer name. No production server is targeted.

IIS site and app pool: OYA_NEW_UI. Binding: HTTP *:8080 with no hostname. Physical path: C:\APPLICATION\OYA_NEW_UI\site. Private/domain Windows Firewall profiles allow TCP 8080. On the server browse http://localhost:8080/Authentication/Login; from its network use http://192.168.1.5:8080/Authentication/Login.

The .NET 8 Hosting Bundle and IIS are installed. The dedicated app pool uses ApplicationPoolIdentity, no managed CLR, and profile/environment loading enabled. It has read access to the application and write access only to App_Data. The dedicated GitHub runner is a NETWORK SERVICE Windows service with modify access to its deployment folder and runner work area, not IIS administration.

Private appsettings.json and Templates are provisioned on OYA-DEV01, not uploaded to GitHub or artifacts. Deployments preserve appsettings*.json, Templates, and App_Data. The initial settings backup is C:\APPLICATION\OYA_NEW_UI\private\appsettings.json. Maintain private settings locally and keep backups protected. TemplateSync remains disabled, so launching this frontend does not start the automatic SQL template updater. Production runtime keeps the Development-only localhost:60474 test login disabled.

The workflow publishes a self-contained win-x64 application, validates that private runtime files are absent from its artifact, and records the commit in wwwroot/deployment-version.json. Deployment briefly places only this site offline, backs up previous application files under C:\APPLICATION\OYA_NEW_UI\backups, updates the application, then checks the login page, JavaScript asset, and exact deployed commit. On failure it restores the prior application files and verifies them. Private settings are preserved and not included in application-file backups. A first deployment with no prior application stays offline if it fails.

GitHub Actions serializes deployments. A filesystem lock prevents concurrent local deployments. Retain sufficient free disk space and manage older backups manually after verifying them; this pipeline does not delete backup folders.

The originally bundled KeyVault client secret was expired. Deployments now supply replacement Azure credentials from encrypted Actions secrets; Key Vault access must be granted to that application. Microsoft sign-in separately requires valid AzureAd credentials and an approved HTTPS callback URL in the existing app registration. HTTP port 8080 alone does not configure a production SSO callback. Passing deployment health checks verifies hosting and assets, not successful authentication, SQL reporting, or a real QuickBooks sync.

The inherited deployment workflow targeting C:\Website\OYA remains excluded. Only .github/workflows/oya-dev01.yml is enabled in this repository.

Azure credentials are delivered through encrypted repository Actions secrets OYA_AZURE_TENANT_ID, OYA_AZURE_CLIENT_ID, and OYA_AZURE_CLIENT_SECRET. During deployment, the script updates only TenantId, ClientId, and ClientSecret in the AzureAd and KeyVault sections of the site's private appsettings.json and its private settings copy. Original settings are backed up below private\config-backups and restored if deployment fails. The secret value is never committed or included in build artifacts. SQL, PubNub, templates, and other private settings are preserved. Azure permissions and registered callback URLs still need to match the application.
