$ErrorActionPreference = 'Stop'
$workspace = (Resolve-Path (Join-Path $PSScriptRoot '../../..')).Path
$source = Join-Path $workspace '.utmp/BasementPolish/Source'
$art = Join-Path $workspace '.utmp/BasementPreview/Assets/Art/Basement/ArenaPolish/Models'
$manifest = @()
foreach ($assetId in @('industrial_storage_cart','barrel_03','metal_tool_chest','steel_frame_shelves_02')) {
    $data = Get-Content -LiteralPath "$source/$assetId-files.json" -Raw | ConvertFrom-Json
    $target = Join-Path $art $assetId
    New-Item -ItemType Directory -Path $target -Force | Out-Null
    $files = @(@{Name="$assetId.fbx"; Data=$data.fbx.'2k'.fbx})
    foreach ($map in @('Diffuse','nor_gl','arm')) { $files += @{Name="$map.png"; Data=$data.$map.'2k'.png} }
    $info = Invoke-RestMethod "https://api.polyhaven.com/info/$assetId"
    foreach ($entry in $files) {
        $file = Join-Path $target $entry.Name
        if (!(Test-Path -LiteralPath $file) -or (Get-Item -LiteralPath $file).Length -ne $entry.Data.size) { & curl.exe -L --fail --silent --show-error --retry 2 --connect-timeout 20 --max-time 180 --output $file $entry.Data.url; if ($LASTEXITCODE -ne 0) { throw "Download failed" } }
        if ((Get-FileHash -LiteralPath $file -Algorithm MD5).Hash.ToLowerInvariant() -ne $entry.Data.md5) { throw "Hash mismatch: $file" }
        $manifest += [ordered]@{asset=$assetId; page="https://polyhaven.com/a/$assetId"; authors=$info.authors; license='CC0-1.0'; licenseUrl='https://polyhaven.com/license'; file="Models/$assetId/$($entry.Name)"; url=$entry.Data.url; sha256=(Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToLowerInvariant()}
    }
    Write-Output "Downloaded and verified $assetId"
}
$manifest | ConvertTo-Json -Depth 12 | Set-Content (Join-Path (Split-Path $art) 'asset-sources.json') -Encoding UTF8
Invoke-WebRequest 'https://media.moddb.com/images/mods/1/26/25250/hotel.jpg' -OutFile "$source/TotalChaos-Hotel.jpg" -UseBasicParsing
Invoke-WebRequest 'https://media.moddb.com/images/mods/1/26/25250/shaders.jpg' -OutFile "$source/TotalChaos-Shaders.jpg" -UseBasicParsing

