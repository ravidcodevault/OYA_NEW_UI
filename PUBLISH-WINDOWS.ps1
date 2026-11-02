param([string]$Destination = (Join-Path $PSScriptRoot 'PublishedApp'))
$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath $PSScriptRoot
& dotnet publish OYA_Web_Application.csproj --configuration Release --runtime win-x64 --self-contained true --output $Destination --nologo -p:DebugType=None -p:DebugSymbols=false
if ($LASTEXITCODE) { throw 'Publish failed.' }
Write-Host ('Published application: '+[IO.Path]::GetFullPath($Destination))
Write-Host 'The published app includes private runtime settings. Keep it private.'
