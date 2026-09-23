$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$front = Join-Path $root 'frontend'
$back = Join-Path $root 'backend\FlowDesk.Api'
$wwwroot = Join-Path $back 'wwwroot'
$out = Join-Path $root 'release\FlowDesk'
Set-Location $front
if (-not (Test-Path 'node_modules')) { npm install }
npm run build
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item (Join-Path $front 'dist\*') $wwwroot -Recurse -Force
Set-Location $back
if (Test-Path $out) { Remove-Item $out -Recurse -Force }
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o $out
Write-Host "Release created in: $out" -ForegroundColor Green
