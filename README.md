# OYA New UI

ASP.NET Core 8 frontend source imported from the configured OYA-DEV01 application.

Private appsettings.json, published binaries, runtime logs, report workbooks, and the inherited deployment workflow are intentionally excluded from version control. They remain available in the local application folder.

For a fresh checkout, copy appsettings.example.json to appsettings.json and supply the environment-specific SQL, Azure, and PubNub settings privately. Copy the required report workbooks into Templates before generating reports. See SETUP-OYA-DEV01.md for workbook/runtime requirements.

Run RUN-LOCAL.cmd with the .NET 8 SDK (or a supported newer SDK). Use PUBLISH-WINDOWS.ps1 to build PublishedApp, then RUN-PUBLISHED.cmd to launch that build. The existing PublishedApp in the original transfer folder can be launched directly.

No production deployment workflow is enabled in this repository.
