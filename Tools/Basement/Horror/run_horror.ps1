param([ValidateSet('build','validate')][string]$Phase = 'build')
$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$previewRoot = Join-Path $projectRoot '.utmp/BasementPreview'
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.5.7f1/Editor/Unity.exe'
$previewLock = Join-Path $previewRoot 'Temp/UnityLockfile'
if (Test-Path -LiteralPath $previewLock) {
    try { $handle = [System.IO.File]::Open($previewLock,'Open','Read','None'); $handle.Dispose() }
    catch { throw 'The isolated editor is still open' }
}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BasementArenaHorrorBuilder.cs') -Destination (Join-Path $previewRoot 'Assets/Editor/BasementArenaHorrorBuilder.cs')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Tools/Basement/Surface/BasementSurfaceBuilder.cs') -Destination (Join-Path $previewRoot 'Assets/Editor/BasementSurfaceBuilder.cs')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Tools/Basement/Surface/BasementSurfacePlayVerifier.cs') -Destination (Join-Path $previewRoot 'Assets/Editor/BasementSurfacePlayVerifier.cs')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/TotalChaos/Runtime/TCFlickerLight.cs') -Destination (Join-Path $previewRoot 'Assets/TotalChaos/Runtime/TCFlickerLight.cs')
Copy-Item -LiteralPath (Join-Path $projectRoot 'Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs') -Destination (Join-Path $previewRoot 'Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs')
$steps = if ($Phase -eq 'build') { @('build') } else { @('final','play') }
if ($Phase -eq 'build') {
    & python (Join-Path $PSScriptRoot 'pose_human.py')
    if ($LASTEXITCODE -ne 0) { throw 'Anatomical body generation failed' }
    & python (Join-Path $PSScriptRoot 'cut_human.py')
    if ($LASTEXITCODE -ne 0) { throw 'Anatomical body dismemberment failed' }
    foreach ($asset in @('Assets/prefabs/Scenario/Basement.prefab','Assets/Art/Basement/BasementFinalArena.prefab')) {
        Copy-Item -LiteralPath (Join-Path $projectRoot ".utmp/Basement-arena-horror/before/$asset") -Destination (Join-Path $previewRoot $asset)
    }
}
if ($Phase -eq 'validate') {
    & python (Join-Path $PSScriptRoot 'install_horror.py')
    if ($LASTEXITCODE -ne 0) { throw 'Arena staging failed' }
}
foreach ($step in $steps) {
    $method = switch ($step) {
        'build' { 'BasementArenaHorrorBuilder.Build' }
        'final' { 'BasementArenaHorrorBuilder.FinalCheck' }
        'play' { 'BasementSurfacePlayVerifier.Run' }
    }
    $argsList = @('-batchmode','-projectPath',$previewRoot,'-executeMethod',$method,'-logFile',(Join-Path $projectRoot ".utmp/Basement-arena-horror-$step.log"))
    if ($step -ne 'play') { $argsList += '-quit' }
    $process = Start-Process -FilePath $unityEditor -ArgumentList $argsList -WindowStyle Hidden -PassThru -Wait
    if ($process.ExitCode -ne 0) { throw "Unity $step failed; exit $($process.ExitCode)" }
    Write-Output "Arena $step completed"
}
