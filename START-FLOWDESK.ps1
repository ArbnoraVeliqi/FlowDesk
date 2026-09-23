$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$front = Join-Path $root 'frontend'
$back = Join-Path $root 'backend\FlowDesk.Api'
$wwwroot = Join-Path $back 'wwwroot'

Write-Host 'FlowDesk - preparing frontend...' -ForegroundColor Cyan
Set-Location $front
if (-not (Test-Path 'node_modules')) { npm install }
npm run build

Write-Host 'Copying frontend into ASP.NET application...' -ForegroundColor Cyan
if (Test-Path $wwwroot) { Remove-Item $wwwroot -Recurse -Force }
New-Item -ItemType Directory -Path $wwwroot | Out-Null
Copy-Item (Join-Path $front 'dist\*') $wwwroot -Recurse -Force

Write-Host 'Restoring and building backend...' -ForegroundColor Cyan
Set-Location $back
dotnet restore
dotnet build -c Release

Write-Host ''
Write-Host 'Starting FlowDesk at http://localhost:5000' -ForegroundColor Green
Write-Host 'Login: admin@flowdesk.dev / Demo123!' -ForegroundColor Yellow
Start-Process 'http://localhost:5000'
dotnet run -c Release --no-build --urls http://localhost:5000
