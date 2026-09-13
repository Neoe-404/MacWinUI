$ErrorActionPreference = 'Stop'
$requiredSdk = '8.0.424'
Write-Host "Checking .NET SDK..."
dotnet --info
dotnet --list-sdks
dotnet --list-runtimes
if ((dotnet --list-sdks) -notmatch [regex]::Escape($requiredSdk)) {
    throw "Required .NET SDK $requiredSdk is not installed."
}
Write-Host "Restoring solution..."
dotnet restore .\MacWinUI.sln --configfile .\NuGet.Config
Write-Host "Building solution..."
dotnet build .\MacWinUI.sln -c Release --no-restore
Write-Host "Running tests..."
dotnet test .\MacWinUI.sln -c Release --no-build --no-restore
Write-Host "Verification passed."
