<#
Computes the release tag/title/notes for the current source tree.

Mirrors the version logic in yaf_dnn/BuildScripts/ModulePackage.targets:
  - The "v3.2.15" part comes from the AssemblyVersion baked into the YAFNET
    submodule (yafsrc/GlobalAssemblyInfo.cs), with the trailing revision
    segment (".0") dropped.
  - The "-6143" build suffix comes from the DNN package manifest
    (yaf_dnn/Installation/YAF.DotNetNuke.Module.dnn), whose version attribute
    is "NN.NNN.00BBBBBB" - only the digits after the "00" are used.

Writes tag / title / notesFile to $env:GITHUB_OUTPUT when running in Actions.
#>

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path "$PSScriptRoot/../.."

$globalAssemblyInfoPath = Join-Path $repoRoot "yafsrc/GlobalAssemblyInfo.cs"
$globalAssemblyInfo = Get-Content $globalAssemblyInfoPath -Raw
if ($globalAssemblyInfo -notmatch 'AssemblyVersion\("(?<version>\d+\.\d+\.\d+)\.\d+"\)') {
    throw "Could not find AssemblyVersion in $globalAssemblyInfoPath"
}
$shortVersion = $Matches.version

$dnnManifestPath = Join-Path $repoRoot "yaf_dnn/Installation/YAF.DotNetNuke.Module.dnn"
[xml]$dnnXml = Get-Content $dnnManifestPath
$package = $dnnXml.dotnetnuke.packages.package |
    Where-Object { $_.name -like 'YetAnotherForumDotNet*' } |
    Select-Object -First 1
$dnnVersion = $package.version
if ($dnnVersion -notmatch '^\d{2}\.\d{3}\.00(?<build>\d+)$') {
    throw "Unexpected version format '$dnnVersion' in $dnnManifestPath"
}
$buildNumber = $Matches.build

$tag = "v$shortVersion-$buildNumber"
$title = "YAFNET-DNN v$shortVersion"

$changesPath = Join-Path $repoRoot "YAFNET/CHANGES.md"
$lines = Get-Content $changesPath
$heading = "# YetAnotherForum.NET v$shortVersion"
$startIndex = [array]::IndexOf($lines, $heading)
if ($startIndex -lt 0) {
    throw "Could not find heading '$heading' in $changesPath"
}
$notesLines = @()
for ($i = $startIndex + 1; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match '^#\s') { break }
    $notesLines += $lines[$i]
}
$notes = ($notesLines -join "`n").Trim()

$notesFile = Join-Path $repoRoot "release-notes.md"
Set-Content -Path $notesFile -Value $notes -NoNewline

Write-Host "Tag:        $tag"
Write-Host "Title:      $title"
Write-Host "Notes file: $notesFile"
Write-Host "---"
Write-Host $notes

if ($env:GITHUB_OUTPUT) {
    "tag=$tag" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "title=$title" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
    "notesFile=$notesFile" | Out-File -FilePath $env:GITHUB_OUTPUT -Append -Encoding utf8
}
