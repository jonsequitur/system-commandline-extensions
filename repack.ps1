
Push-Location $PSScriptRoot
$ErrorActionPreference = "Stop"

try
{
    # clean up the previously-cached NuGet packages
    $nugetCachePath = $env:NUGET_PACKAGES
    if (-not $nugetCachePath) {
        $nugetCachePath = "~\.nuget\packages"
    }
    Remove-Item -Recurse "$nugetCachePath\HelpLine.*" -Force

    # build and pack dotnet-interactive 
    dotnet clean -c debug
    dotnet pack src/HelpLine.Docs -c debug /p:PackageVersion="0.1.0-dev"
    dotnet pack src/HelpLine.HelpBuilder -c debug /p:PackageVersion="0.1.0-dev"

    # copy the HelpLine packages to the temp directory
    $destinationPath = "q:\temp\packages"
    if (-not (Test-Path -Path $destinationPath -PathType Container)) {
        New-Item -Path $destinationPath -ItemType Directory -Force
    }
    Get-ChildItem -Recurse -Filter *.nupkg | Move-Item -Destination $destinationPath -Force
}
finally
{
    Pop-Location
}
