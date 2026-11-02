param([switch]$Published)
$ErrorActionPreference = 'Stop'
$appRoot = if ($Published) { Join-Path $PSScriptRoot 'PublishedApp' } else { $PSScriptRoot }
Set-Location -LiteralPath $appRoot
if (-not $Published -and -not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    throw 'Install the .NET 8 SDK to run this source project.'
}
try {
    $settings = Get-Content -LiteralPath (Join-Path $appRoot 'appsettings.json') -Raw | ConvertFrom-Json
} catch { throw 'appsettings.json is invalid; private values are not displayed.' }
foreach ($name in @('PublishKey','SubscribeKey','SecretKey')) {
    $value = [string]$settings.PubNub.$name
    if ([string]::IsNullOrWhiteSpace($value)) { throw 'The PubNub keyset is incomplete.' }
    [Environment]::SetEnvironmentVariable(('PubNub__'+$name),$value,'Process')
}
$env:PubNub__AuthToken = ''
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$env:DOTNET_ENVIRONMENT = 'Development'
$env:TemplateSync__Enabled = 'false'
Write-Host 'Starting OYA at http://localhost:60474. Keep this window open.'
if ($Published) {
    & (Join-Path $appRoot 'OYA_Web_Application.exe') --urls 'http://localhost:60474'
} else {
    & dotnet run --project (Join-Path $PSScriptRoot 'OYA_Web_Application.csproj') --no-launch-profile --urls 'http://localhost:60474'
}
exit $LASTEXITCODE
