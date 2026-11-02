# OYA configured source package

Extract the complete folder on OYA-DEV01, for example under C:\Users\Administrator\DEVELOPERS. Paths to packaged workbooks and generated reports resolve from the extracted application folder.

## Run locally

For the supplied self-contained Windows x64 build, run RUN-PUBLISHED.cmd; an SDK is not needed. To run or edit the source, install the .NET 8 SDK and run RUN-LOCAL.cmd instead. Open http://localhost:60474. Run only one launcher at a time. The launchers bind only to localhost and use Development mode. The existing local developer login is restricted to this port and loopback connections. Microsoft sign-in requires a valid AzureAd configuration and a registered callback URI.

The configured SQL database must be reachable from OYA-DEV01. Connect its normal network/VPN before using dashboards and reports. This package does not copy the SQL database, create tables, or run migrations.

## Configuration

- appsettings.json includes the requested matching PubNub publish, subscribe and secret keys. The stale AuthToken is cleared so the app obtains fresh scoped tokens. Commands use OYA_SYNC_UPDATE and OYA_WEB_USER, matching the backend.
- The backend must be running with the same keyset. Start Sync requests a real backend sync; the package does not start one automatically.
- Templates contains the seven workbooks referenced by this project. App_Data/Reports and App_Data/Logs are created when the app starts.
- Automatic template-to-database distributor updates are disabled by TemplateSync:Enabled=false. Set it to true only if you intend those database updates.
- Data Protection uses framework defaults, including under IIS. No user-profile DPAPI configuration is forced. Do not enable ephemeral protection for production.
- Existing AzureAd values are preserved. Azure Key Vault settings are included separately under KeyVault. The KeyVault client secret recovered from the old source was confirmed expired by Azure (AADSTS7000222). Replace KeyVault:ClientSecret with its new secret value before production username/password sign-in. KeyVault__ClientSecret can override it via the environment. AZURE_CLIENT_SECRET is used when the KeyVault setting is blank.
- AzureAd:ClientSecret is a separate Microsoft sign-in setting and has not been confirmed valid. Do not confuse it with the PubNub secret or KeyVault credential.

## Publish for Windows / IIS

Run PUBLISH-WINDOWS.ps1 to rebuild the self-contained Windows x64 PublishedApp folder. The Templates, libs dependencies, static assets and private appsettings.json are included. The source and PublishedApp have separate copies of appsettings.json; update both as appropriate, or rebuild after configuration changes. IIS requires the ASP.NET Core Hosting Bundle. Give the app-pool identity write access only to the app's App_Data folder if using file outputs, and configure valid Azure settings before switching a production site. The local launchers intentionally keep automatic template database updates disabled.

The pipeline and the live production IIS site are not changed by preparing this package. Keep the working release available for rollback when deploying.

## Private archive

This ZIP contains credentials and report workbooks. Transfer it only to your intended OYA-DEV01 machine. Do not upload appsettings.json or this configured archive to a public repository. It includes source and the freshly built PublishedApp. Older bin/obj output, logs, old ZIPs, editor caches and Git history are excluded.
