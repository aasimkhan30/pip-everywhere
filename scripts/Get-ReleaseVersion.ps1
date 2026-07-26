[CmdletBinding()]
param(
    [ValidateRange(1, [int]::MaxValue)]
    [int]$MinimumPatch = 7
)

$ErrorActionPreference = 'Stop'

function Get-Patches {
    param([string[]]$Tags)

    @(
        $Tags | ForEach-Object {
            if ($_ -match '^v0\.0\.(\d+)$') {
                [int]$Matches[1]
            }
        }
    )
}

$currentPatches = Get-Patches @(git tag --points-at HEAD --list 'v0.0.*')
if ($LASTEXITCODE -ne 0) {
    throw 'Unable to read version tags for the current commit.'
}

if ($currentPatches.Count -gt 0) {
    $patch = ($currentPatches | Measure-Object -Maximum).Maximum
}
else {
    $allPatches = Get-Patches @(git tag --list 'v0.0.*')
    if ($LASTEXITCODE -ne 0) {
        throw 'Unable to read repository version tags.'
    }

    $nextTaggedPatch = if ($allPatches.Count -gt 0) {
        ($allPatches | Measure-Object -Maximum).Maximum + 1
    }
    else {
        1
    }
    $patch = [Math]::Max($MinimumPatch, $nextTaggedPatch)
}

[pscustomobject]@{
    Release = "0.0.$patch"
    Package = "0.0.$patch.0"
}

