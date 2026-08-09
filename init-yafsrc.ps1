<#
Sets up the yafsrc/ directory required to build this solution.

The YAFNET (netfx branch) source lives in this repo as the "YAFNET" git
submodule. Since its build output expects a sibling folder named
"yafsrc" (see yaf_dnn/YAF.DNN.Module.csproj), this script creates an
NTFS directory junction yafsrc -> YAFNET/yafsrc so no project paths
need to change.

Run this once after cloning (and after every "git submodule update"
that changes the YAFNET commit, though the junction itself does not
need to be recreated for that).
#>

$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

Write-Host "Initializing YAFNET submodule..."
git submodule update --init --recursive

$target = Join-Path $PSScriptRoot "YAFNET\yafsrc"
if (-not (Test-Path $target)) {
    throw "Expected submodule path not found: $target"
}

$link = Join-Path $PSScriptRoot "yafsrc"
if (Test-Path $link) {
    Write-Host "yafsrc already exists, skipping junction creation."
} else {
    New-Item -ItemType Junction -Path $link -Target $target | Out-Null
    Write-Host "Created junction: yafsrc -> YAFNET\yafsrc"
}

Write-Host "Done. You can now open and build the YAFNET and YAFNET-DNN solutions."
