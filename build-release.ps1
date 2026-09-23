# 悬浮键鼠助手 - 发布 + 生成安装包
# 用法：powershell -ExecutionPolicy Bypass -File build-release.ps1

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Version,
    [string]$InnoSetupDir,
    [switch]$FrameworkDependent
)

$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
$Csproj = Join-Path $Root 'src\FloatingKeypad\FloatingKeypad.csproj'
$Iss = Join-Path $Root 'installer\FloatingKeypad.iss'
$PublishDir = 'D:\FloatingKeypad\publish'
$InstallerDir = 'D:\FloatingKeypad\installer'

if (-not (Test-Path $Csproj)) { throw "找不到项目文件: $Csproj" }
if (-not (Test-Path $Iss)) { throw "找不到安装脚本: $Iss" }

if (-not $Version) {
    [xml]$xml = Get-Content -LiteralPath $Csproj -Raw
    $Version = $xml.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
}
if (-not $Version) { throw "无法从 csproj 读取 <Version>，请用 -Version 指定" }
Write-Host "版本号: $Version" -ForegroundColor Cyan

function Resolve-Tool([string]$Name, [string[]]$Candidates) {
    $cmd = Get-Command $Name -ErrorAction SilentlyContinue
    if ($cmd) { return $cmd.Source }
    foreach ($c in $Candidates) { if ($c -and (Test-Path $c)) { return $c } }
    throw "找不到 $Name，请安装或通过参数指定路径"
}

$Dotnet = Resolve-Tool 'dotnet.exe' @(
    "$env:ProgramFiles\dotnet\dotnet.exe",
    "${env:ProgramFiles(x86)}\dotnet\dotnet.exe"
)

$IsccCandidates = @(
    "$InnoSetupDir\ISCC.exe",
    'D:\InnoSetup\ISCC.exe',
    "$env:ProgramFiles\Inno Setup 6\ISCC.exe",
    "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
    "$env:ProgramFiles\Inno Setup 5\ISCC.exe"
)
$Iscc = Resolve-Tool 'ISCC.exe' $IsccCandidates

$selfContained = if ($FrameworkDependent) { 'false' } else { 'true' }
if ($FrameworkDependent) {
    Write-Warning '框架依赖模式：安装包不再内置 .NET，目标机需预装 .NET 8 Desktop Runtime'
}

Write-Host "`n[1/2] 发布 ($(if ($FrameworkDependent) {'框架依赖'} else {'自包含'}) win-x64) ..." -ForegroundColor Cyan
if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

& $Dotnet publish $Csproj -c $Configuration -r win-x64 --self-contained $selfContained -o $PublishDir -p:Version=$Version
if ($LASTEXITCODE -ne 0) { throw "dotnet publish 失败 ($LASTEXITCODE)" }

$exe = Join-Path $PublishDir 'FloatingKeypad.exe'
if (-not (Test-Path $exe)) { throw "发布产物缺失: $exe" }

Write-Host "`n[2/2] 编译安装包 ..." -ForegroundColor Cyan
New-Item -ItemType Directory -Force -Path $InstallerDir | Out-Null
& $Iscc "/DAppVersion=$Version" $Iss
if ($LASTEXITCODE -ne 0) { throw "ISCC 编译失败 ($LASTEXITCODE)" }

$setup = Join-Path $InstallerDir "FloatingKeypad-Setup-$Version.exe"
Write-Host "`n完成:" -ForegroundColor Green
Write-Host "  发布目录 : $PublishDir"
Write-Host "  安装包   : $setup"
