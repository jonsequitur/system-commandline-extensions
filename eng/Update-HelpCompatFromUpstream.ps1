[CmdletBinding()]
param(
    [string]$Ref,
    [string]$Repository = "https://raw.githubusercontent.com/dotnet/command-line-api"
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = Split-Path -Parent $PSScriptRoot
$manifestPath = Join-Path $PSScriptRoot 'help-sync.json'
$manifest = Get-Content -Raw -Path $manifestPath | ConvertFrom-Json

if ([string]::IsNullOrWhiteSpace($Ref)) {
    $Ref = $manifest.defaultRef
}

if ($manifest.notes) {
    foreach ($note in $manifest.notes) {
        Write-Host "Note: $note"
    }
}

foreach ($file in $manifest.files) {
    $uri = '{0}/{1}/{2}' -f $Repository.TrimEnd('/'), $Ref, $file.upstreamPath
    $destinationPath = Join-Path $repoRoot $file.destination
    $destinationDirectory = Split-Path -Parent $destinationPath

    if (-not (Test-Path -LiteralPath $destinationDirectory)) {
        New-Item -ItemType Directory -Path $destinationDirectory -Force | Out-Null
    }

    Write-Host "Syncing $($file.upstreamPath) -> $($file.destination)"
    $content = (Invoke-WebRequest -UseBasicParsing -Uri $uri).Content

    foreach ($replacement in $file.replacements) {
        $content = $content.Replace([string]$replacement.old, [string]$replacement.new)
    }

    [System.IO.File]::WriteAllText($destinationPath, $content, [System.Text.UTF8Encoding]::new($false))
}

Write-Host "HelpCompat sync completed from ref '$Ref'."
