param(
    [Parameter(Mandatory=$true)][string]$PackagePath,
    [Parameter(Mandatory=$true)][string]$ExpectedCommit
)
$ErrorActionPreference = 'Stop'
if ($env:COMPUTERNAME -ne 'OYA-DEV01') { throw 'This deployment may only run on OYA-DEV01.' }
if ($ExpectedCommit -notmatch '^[a-f0-9]{40}$') { throw 'Invalid commit identifier.' }
$root = 'C:\APPLICATION\OYA_NEW_UI'
$site = Join-Path $root 'site'
$backup = Join-Path $root ('backups\' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '-' + $ExpectedCommit.Substring(0,8))
$source = [IO.Path]::GetFullPath((Join-Path $PackagePath 'app'))
if (-not (Test-Path (Join-Path $source 'OYA_Web_Application.dll'))) { throw 'Published application is missing.' }
if (-not (Test-Path (Join-Path $source 'web.config'))) { throw 'IIS web.config is missing.' }
$marker = Get-Content (Join-Path $source 'wwwroot\deployment-version.json') -Raw | ConvertFrom-Json
if ($marker.commit -ne $ExpectedCommit) { throw 'Artifact commit does not match this workflow.' }
foreach ($path in @('appsettings.json','appsettings.Production.json','Templates','App_Data')) {
    if (Test-Path (Join-Path $source $path)) { throw ('Artifact must not contain private runtime content: ' + $path) }
}
$configPath = Join-Path $site 'appsettings.json'
if (-not (Test-Path $configPath)) { throw 'Provision private appsettings.json on OYA-DEV01 before deploying.' }
$configHash = (Get-FileHash $configPath -Algorithm SHA256).Hash
$azureSettings = @{
    TenantId = $env:OYA_AZURE_TENANT_ID
    ClientId = $env:OYA_AZURE_CLIENT_ID
    ClientSecret = $env:OYA_AZURE_CLIENT_SECRET
}
foreach ($key in $azureSettings.Keys) {
    if ([string]::IsNullOrWhiteSpace($azureSettings[$key])) { throw 'Required Azure Actions secrets are missing.' }
}
foreach ($key in @('TenantId','ClientId')) {
    $parsedGuid = [guid]::Empty
    if (-not [guid]::TryParse($azureSettings[$key],[ref]$parsedGuid)) { throw 'Azure application identifiers are invalid.' }
}
$privateSettingsPath = Join-Path $root 'private\appsettings.json'
if (-not (Test-Path $privateSettingsPath)) { throw 'Private settings backup is missing.' }
$configBackupDir = Join-Path $root ('private\config-backups\deploy-' + [guid]::NewGuid().ToString('N'))
function Set-AzureSettings([string]$Path) {
    try { $settings = Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json }
    catch { throw 'Private runtime settings are invalid JSON.' }
    foreach ($section in @('AzureAd','KeyVault')) {
        if (-not $settings.$section) { throw 'Expected Azure runtime settings section is missing.' }
        foreach ($key in $azureSettings.Keys) { $settings.$section.$key = $azureSettings[$key] }
    }
    $temp = $Path + '.azure-deploy.tmp'
    [IO.File]::WriteAllText($temp,($settings | ConvertTo-Json -Depth 32),[Text.UTF8Encoding]::new($false))
    Move-Item -LiteralPath $temp -Destination $Path -Force
}
$offline = Join-Path $site 'app_offline.htm'
if (Test-Path $offline) { throw 'The site already has app_offline.htm; inspect before deploying.' }
$lockPath = Join-Path $root 'deploy.lock'
$lock = [IO.File]::Open($lockPath,[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
function Copy-App([string]$From,[string]$To) {
    & robocopy $From $To /MIR /XD Templates App_Data /XF 'appsettings*.json' app_offline.htm /R:5 /W:2 /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw ('Application copy failed; robocopy exit ' + $LASTEXITCODE) }
    $global:LASTEXITCODE = 0
}
function Test-Site([string]$Commit) {
    for ($attempt=0; $attempt -lt 24; $attempt++) {
        try {
            $login = Invoke-WebRequest 'http://localhost:8080/Authentication/Login' -UseBasicParsing -TimeoutSec 10
            if ($login.StatusCode -ne 200) { throw 'Login page is unavailable.' }
            $asset = Invoke-WebRequest 'http://localhost:8080/assets/js/scripts.bundle.js' -UseBasicParsing -TimeoutSec 10
            if ($asset.StatusCode -ne 200) { throw 'JavaScript asset is unavailable.' }
            if ($Commit) {
                $version = Invoke-RestMethod ('http://localhost:8080/deployment-version.json?commit=' + $Commit) -TimeoutSec 10
                if ($version.commit -ne $Commit) { throw 'IIS is serving a different commit.' }
            }
            return
        } catch {
            if ($attempt -eq 23) { throw 'IIS health validation failed.' }
            Start-Sleep -Seconds 2
        }
    }
}
$hasPrevious = Test-Path (Join-Path $site 'OYA_Web_Application.dll')
$previousCommit = ''
if (Test-Path (Join-Path $site 'wwwroot\deployment-version.json')) {
    $previousCommit = (Get-Content (Join-Path $site 'wwwroot\deployment-version.json') -Raw | ConvertFrom-Json).commit
}
$backupReady = $false
$mutated = $false
try {
    Set-Content -LiteralPath $offline -Value '<html><body>OYA update in progress. Please refresh shortly.</body></html>' -Encoding UTF8
    Start-Sleep -Seconds 5
    if ($hasPrevious) {
        New-Item -ItemType Directory -Path $backup -Force | Out-Null
        Copy-App $site $backup
        $backupReady = $true
    }
    New-Item -ItemType Directory -Path $configBackupDir -Force | Out-Null
    Copy-Item -LiteralPath $configPath -Destination (Join-Path $configBackupDir 'site-appsettings.json')
    Copy-Item -LiteralPath $privateSettingsPath -Destination (Join-Path $configBackupDir 'private-appsettings.json')
    $mutated = $true
    Set-AzureSettings $configPath
    Set-AzureSettings $privateSettingsPath
    $configHash = (Get-FileHash $configPath -Algorithm SHA256).Hash
    Copy-App $source $site
    if ((Get-FileHash $configPath -Algorithm SHA256).Hash -ne $configHash) { throw 'Private settings changed during deployment.' }
    Remove-Item -LiteralPath $offline -Force
    Test-Site $ExpectedCommit
    Write-Host ('Deployed ' + $ExpectedCommit + ' to OYA-DEV01 at http://localhost:8080.')
} catch {
    $failure = $_
    foreach ($restore in @(
        @{Backup='site-appsettings.json';Target=$configPath},
        @{Backup='private-appsettings.json';Target=$privateSettingsPath}
    )) {
        $saved = Join-Path $configBackupDir $restore.Backup
        if (Test-Path $saved) { Copy-Item -LiteralPath $saved -Destination $restore.Target -Force }
    }
    if ($mutated -and $backupReady) {
        Set-Content -LiteralPath $offline -Value 'Restoring previous OYA release.' -Encoding UTF8
        Start-Sleep -Seconds 5
        Copy-App $backup $site
        Remove-Item -LiteralPath $offline -Force
        Test-Site $previousCommit
        Write-Warning 'Deployment failed. Previous application files were restored and passed health checks.'
    } elseif (-not $mutated -and (Test-Path $offline)) {
        Remove-Item -LiteralPath $offline -Force
    } elseif ($mutated) {
        Set-Content -LiteralPath $offline -Value 'OYA deployment failed. Administrator action is required.' -Encoding UTF8
    }
    throw $failure
} finally {
    $lock.Dispose()
}
exit 0
