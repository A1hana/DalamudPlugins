param(
    [string]$DalamudLibPath,
    [string]$Dotnet = 'dotnet'
)
$ErrorActionPreference = 'Stop'
$projectRoot = $PSScriptRoot
$repositoryUrl = 'https://github.com/A1hana/DalamudPlugins'
Push-Location -LiteralPath $projectRoot
try {
    $buildArgs = @('build', 'src/SamuraiPvp/SamuraiPvp.csproj', '-c', 'Release')
    if ($DalamudLibPath) {
        $libraryPath = (Resolve-Path -LiteralPath $DalamudLibPath).Path.TrimEnd('\', '/') + '/'
        $buildArgs += "-p:DalamudLibPath=$libraryPath"
    }
    & $Dotnet @buildArgs
    if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }

    $releasePath = Join-Path $projectRoot 'src/SamuraiPvp/bin/Release'
    $manifest = Get-Content -LiteralPath (Join-Path $releasePath 'SamuraiPvp.json') -Raw | ConvertFrom-Json
    $version = $manifest.AssemblyVersion
    if ($version -notmatch '^\d+\.\d+\.\d+\.\d+$' -or $manifest.DalamudApiLevel -ne 15) {
        throw 'Unexpected plugin version or Dalamud API level.'
    }
    $releaseTag = "SamuraiPvp-v$version"
    $downloadUrl = "$repositoryUrl/releases/download/$releaseTag/latest.zip"
    $distributionPath = Join-Path $projectRoot 'dist'
    New-Item -ItemType Directory -Path $distributionPath -Force | Out-Null

    # Archive inputs are explicit so build intermediates never enter the installer.
    $packageFiles = @('SamuraiPvp.dll', 'SamuraiPvp.json', 'SamuraiPvp.deps.json') |
        ForEach-Object { Join-Path $releasePath $_ }
    $packageFiles += @('README.md', 'NOTICE', 'LICENSE.md') |
        ForEach-Object { Join-Path $projectRoot $_ }
    $zipPath = Join-Path $distributionPath 'latest.zip'
    Compress-Archive -LiteralPath $packageFiles -DestinationPath $zipPath -Force

    $entry = [ordered]@{
        Author = $manifest.Author
        Name = $manifest.Name
        InternalName = $manifest.InternalName
        AssemblyVersion = $version
        Punchline = $manifest.Punchline
        Description = $manifest.Description
        RepoUrl = $repositoryUrl
        ApplicableVersion = $manifest.ApplicableVersion
        DalamudApiLevel = $manifest.DalamudApiLevel
        Tags = $manifest.Tags
        IsHide = $false
        IsTestingExclusive = $false
        DownloadLinkInstall = $downloadUrl
        DownloadLinkUpdate = $downloadUrl
        DownloadLinkTesting = $downloadUrl
        LastUpdate = [DateTimeOffset]::UtcNow.ToUnixTimeSeconds()
    }
    $indexPath = Join-Path $projectRoot 'pluginmaster.json'
    $entries = @()
    if (Test-Path -LiteralPath $indexPath) {
        $entries = @(Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json |
            Where-Object { $_.InternalName -ne $manifest.InternalName })
    }
    $entries += $entry
    $indexJson = ConvertTo-Json -InputObject @($entries) -Depth 10
    [System.IO.File]::WriteAllText($indexPath, $indexJson + [Environment]::NewLine, [System.Text.UTF8Encoding]::new($false))

    $hash = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash
    [System.IO.File]::WriteAllText((Join-Path $distributionPath 'SHA256SUMS.txt'), "$hash  latest.zip`n")
    Write-Output "Package: $zipPath"
    Write-Output "Release tag: $releaseTag"
    Write-Output 'Upload latest.zip to this release before publishing pluginmaster.json.'
} finally {
    Pop-Location
}
