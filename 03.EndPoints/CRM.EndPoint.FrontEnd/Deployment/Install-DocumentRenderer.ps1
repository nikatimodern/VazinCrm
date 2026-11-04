#Requires -Version 5.1
<#
.SYNOPSIS
Installs the published Playwright browser for an IIS application pool.
.EXAMPLE
.\Deployment\Install-DocumentRenderer.ps1 -AppPoolName "VazinCrm"
#>
[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[^\\/:*?"<>|]+$')]
    [string]$AppPoolName,

    [string]$PublishPath = (Split-Path -Parent $PSScriptRoot)
)

$ErrorActionPreference = 'Stop'
$publishRoot = (Resolve-Path -LiteralPath $PublishPath).Path
$webConfigPath = Join-Path $publishRoot 'web.config'
$driverRoot = Join-Path $publishRoot '.playwright'
$cliPath = Join-Path $driverRoot 'package\cli.js'
$nodePath = Join-Path $driverRoot 'node\win32_x64\node.exe'
$browserRoot = Join-Path $env:ProgramData "VazinCrm\Playwright\$AppPoolName"

foreach ($requiredPath in @($webConfigPath, $cliPath, $nodePath)) {
    if (-not (Test-Path -LiteralPath $requiredPath -PathType Leaf)) {
        throw "Missing published file: $requiredPath. Copy the complete Windows x64 publish output, including .playwright."
    }
}

$document = New-Object System.Xml.XmlDocument
$document.PreserveWhitespace = $true
$document.XmlResolver = $null
$document.Load($webConfigPath)
$aspNetCore = $document.SelectSingleNode('/configuration/location[@path="."]/system.webServer/aspNetCore | /configuration/system.webServer/aspNetCore')
if ($null -eq $aspNetCore) {
    throw 'The published web.config does not contain an ASP.NET Core application.'
}

if (-not $PSCmdlet.ShouldProcess($publishRoot, "Install Chromium in $browserRoot, grant IIS pool access, and update web.config")) {
    return
}

$currentUser = [System.Security.Principal.WindowsIdentity]::GetCurrent()
$principal = New-Object System.Security.Principal.WindowsPrincipal($currentUser)
if (-not $principal.IsInRole([System.Security.Principal.WindowsBuiltInRole]::Administrator)) {
    throw 'Run this script in PowerShell as Administrator on the IIS server.'
}
$poolAccount = New-Object System.Security.Principal.NTAccount("IIS AppPool\$AppPoolName")
$null = $poolAccount.Translate([System.Security.Principal.SecurityIdentifier])

$null = New-Item -ItemType Directory -Path $browserRoot -Force
$previousBrowserPath = $env:PLAYWRIGHT_BROWSERS_PATH
try {
    $env:PLAYWRIGHT_BROWSERS_PATH = $browserRoot
    # Use the driver shipped with this application to install the matching browser revision.
    & $nodePath $cliPath install chromium --only-shell
    if ($LASTEXITCODE -ne 0) {
        throw "Playwright browser installation failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:PLAYWRIGHT_BROWSERS_PATH = $previousBrowserPath
}

& icacls.exe $browserRoot /grant "IIS AppPool\${AppPoolName}:(OI)(CI)RX" /T /Q
if ($LASTEXITCODE -ne 0) {
    throw "Unable to grant browser access to IIS AppPool\$AppPoolName."
}

$environmentVariables = $aspNetCore.SelectSingleNode('environmentVariables')
if ($null -eq $environmentVariables) {
    $environmentVariables = $document.CreateElement('environmentVariables')
    $null = $aspNetCore.AppendChild($environmentVariables)
}
$browserVariable = $environmentVariables.SelectSingleNode('environmentVariable[@name="PLAYWRIGHT_BROWSERS_PATH"]')
if ($null -eq $browserVariable) {
    $browserVariable = $document.CreateElement('environmentVariable')
    $browserVariable.SetAttribute('name', 'PLAYWRIGHT_BROWSERS_PATH')
    $null = $environmentVariables.AppendChild($browserVariable)
}
$browserVariable.SetAttribute('value', $browserRoot)
$document.Save($webConfigPath)

Write-Output "Chromium installed in $browserRoot. Recycle the '$AppPoolName' pool, then test PNG and PDF downloads."
