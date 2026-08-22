param(
    [string]$OutputPath = "artifacts\publish"
)

$ErrorActionPreference = "Stop"
$projectRoot = Split-Path -Parent $PSScriptRoot
$solutionPath = Join-Path $projectRoot "MacWinUI.sln"
$appProject = Join-Path $projectRoot "src\MacWinUI.App\MacWinUI.App.csproj"
$publishPath = Join-Path $projectRoot $OutputPath

dotnet build $solutionPath -c Release
dotnet test $solutionPath -c Release --no-build
dotnet publish $appProject -c Release --no-build --no-restore -o $publishPath

Write-Host "MacWinUI release published to: $publishPath"
