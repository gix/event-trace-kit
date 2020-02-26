[CmdletBinding()]
param(
    [Parameter(Mandatory=$true, ParameterSetName='Regenerate')]
    [switch] $Regenerate,

    [Parameter(Mandatory=$true, ParameterSetName='CompareVersions')]
    [switch] $CompareVersions
)

$versions = @(
    '8.1',
    '10240',
    '10586',
    '14393',
    '15063',
    '16299',
    '17763',
    '18362'
)

$manifests = @(
    'ImportChannels',
    'ImportChannelFromProvider',
    'TaskOpcodes',
    'wpf-etw'
)
$manifests2 = @(
    'EventAttributes',
    'Large',
    'ProviderTraits',
    'ReferenceChannels',
    'TypeMangling'
)

if ($Regenerate) {
    mc-8.1 -um -z x "wpf-etw.man"
    if (! $?) { throw "mc failed" }
    Move-Item -Force xTEMP.BIN "wpf-etw.wevt.v3-8.1-compat.bin"
    Move-Item -Force x_MSG00001.bin "wpf-etw.msg.v3-8.1-compat.bin"
    Move-Item -Force x.h "wpf-etw.wevt.v3-8.1-compat.h"

    foreach ($manifest in $manifests) {
        mc-15063 -um -z x "${manifest}.man"
        if (! $?) { throw "mc failed" }
        Move-Item -Force xTEMP.BIN "${manifest}.wevt.v3.bin"

        mc-18362 -um -z x "${manifest}.man"
        if (! $?) { throw "mc failed" }
        Move-Item -Force xTEMP.BIN "${manifest}.wevt.v5.bin"
        Move-Item -Force x_MSG00001.bin "${manifest}.msg.bin" -ErrorAction SilentlyContinue
        Move-Item -Force x.h "${manifest}.h"
    }

    foreach ($manifest in $manifests2) {
        mc-18362 -um -z x "${manifest}.man"
        if (! $?) { throw "mc failed" }
        Move-Item -Force xTEMP.BIN "${manifest}.wevt.v5.bin"
        Move-Item -Force x_MSG00001.bin "${manifest}.msg.bin" -ErrorAction SilentlyContinue
        Move-Item -Force x.h "${manifest}.h"
    }

    Remove-Item x.rc
}

if ($CompareVersions) {
    foreach ($manifest in $manifests) {
        Write-Output "===== $manifest ====="

        $evtItems = @()
        $msgItems = @()
        foreach ($version in $versions) {
            &"mc-${version}.exe" -um -z x "${manifest}.man"
            if ($?) {
                $evtItems += Move-Item -Force -PassThru -ErrorAction SilentlyContinue xTEMP.BIN "~tmp.${manifest}.wevt.v${version}.bin"
                $msgItems += Move-Item -Force -PassThru -ErrorAction SilentlyContinue x_MSG00001.bin "~tmp.${manifest}.msg.v${version}.bin"
            }
        }

        $evtHashes = $evtItems | Get-FileHash -Algorithm SHA1
        $msgHashes = $msgItems | Get-FileHash -Algorithm SHA1

        $evtGroups = @($evtHashes | Group-Object Hash)
        if ($evtGroups.Count -le 1) {
            Write-Host -ForegroundColor Green "All event templates identical"
        } else {
            Write-Host -ForegroundColor Yellow "Different event templates"
            Write-Output $evtHashes
        }

        $msgGroups = @($msgHashes | Group-Object Hash)
        if ($msgGroups.Count -le 1) {
            Write-Host -ForegroundColor Green "All message tables identical"
        } else {
            Write-Host -ForegroundColor Yellow "Different message tables"
            Write-Output $msgHashes
        }

        Remove-Item '~tmp.*.bin'
    }
}
