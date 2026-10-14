$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$previewRoot = Join-Path $projectRoot '.utmp/BasementPreview'
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Unity.exe'
$previewLock = Join-Path $previewRoot 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $previewLock) {
    try {
        $lockHandle = [System.IO.File]::Open($previewLock, 'Open', 'Read', 'None')
        $lockHandle.Dispose()
    } catch {
        throw 'The isolated project is still open. Wait for the active batch run.'
    }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BasementSurfaceBuilder.cs') -Destination (Join-Path $previewRoot 'Assets/Editor/BasementSurfaceBuilder.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BasementSurfacePlayVerifier.cs') -Destination (Join-Path $previewRoot 'Assets/Editor/BasementSurfacePlayVerifier.cs')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs') -Destination (Join-Path $previewRoot 'Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/TotalChaos/Runtime/TCRegionAtmosphere.cs') -Destination (Join-Path $previewRoot 'Assets/TotalChaos/Runtime/TCRegionAtmosphere.cs')
foreach ($step in @('regions', 'final', 'play')) {
    if ($step -eq 'final') {
        & python (Join-Path $PSScriptRoot 'install_surface.py')
        if ($LASTEXITCODE -ne 0) { throw 'Prefab staging failed' }
    }
    $method = switch ($step) {
        'regions' { 'BasementSurfaceBuilder.RepairCorridorRegions' }
        'final' { 'BasementSurfaceBuilder.FinalCheck' }
        'play' { 'BasementSurfacePlayVerifier.Run' }
    }
    $batchArgs = @('-batchmode', '-projectPath', $previewRoot, '-executeMethod', $method,
        '-logFile', (Join-Path $projectRoot ".utmp/Basement-PBR-$step.log"))
    if ($step -ne 'play') { $batchArgs += '-quit' }
    $process = Start-Process -FilePath $unityEditor -ArgumentList $batchArgs -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Unity $step validation failed, exit $($process.ExitCode)" }
    Write-Output "Unity $step validation completed"
}
