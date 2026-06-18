
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

    # build and pack 
    dotnet clean -c debug
    dotnet pack src/HelpLine.Docs -c debug /p:PackageVersion="0.1.0-dev"
    dotnet pack src/HelpLine.HelpBuilder -c debug /p:PackageVersion="0.1.0-dev"
    dotnet pack src/HelpLine.FancyHelp -c debug /p:PackageVersion="0.1.0-dev"

    # copy the HelpLine packages to the temp directory
    $destinationPath = "q:\temp\packages"
    if (Test-Path -Path $destinationPath -PathType Container) {
        Remove-Item "$destinationPath\*.nupkg" -Force
    } else {
        New-Item -Path $destinationPath -ItemType Directory -Force
    }
    Get-ChildItem -Recurse -Filter *.nupkg | Move-Item -Destination $destinationPath -Force

    Write-Host ""
    Write-Host "Packages written to: $destinationPath" -ForegroundColor Green
    Get-ChildItem -Path $destinationPath -Filter *.nupkg | Sort-Object Name | ForEach-Object {
        Write-Host "  $($_.Name)"
    }
}
finally
{
    Pop-Location
}
