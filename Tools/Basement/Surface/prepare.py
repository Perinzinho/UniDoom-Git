"""Snapshot current work and synchronize the isolated Unity project without deleting files."""
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/Basement-PBR/before'
TARGETS = ['Assets/prefabs/Scenario/Basement.prefab', 'Assets/Art/Basement/BasementFinalArena.prefab',
           'Assets/Art/Basement/BasementFinalSanctum.prefab']


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert not (PREVIEW / 'Temp/UnityLockfile').exists(), 'Isolated project is already open'
    assert not (BEFORE / 'manifest.json').exists(), 'Snapshot already exists; do not overwrite it'
    manifest = {}
    copied = 0
    for folder in ['Assets', 'Packages', 'ProjectSettings']:
        for source in (ROOT / folder).rglob('*'):
            if not source.is_file():
                continue
            relative = source.relative_to(ROOT)
            sha = digest(source)
            manifest[relative.as_posix()] = sha
            target = PREVIEW / relative
            if not target.exists() or digest(target) != sha:
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(source, target)
                copied += 1
    for relative in TARGETS + ['Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs']:
        target = BEFORE / relative
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / relative, target)
    (BEFORE / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    guids = {}
    for meta in (ROOT / 'Assets').rglob('*.mat.meta'):
        match = re.search(r'^guid: (\w+)', meta.read_text(), re.M)
        if match:
            guids[match[1]] = str(meta.relative_to(ROOT))[:-5]
    for relative in TARGETS:
        text = (ROOT / relative).read_text(encoding='utf-8-sig')
        materials = sorted({guids[g] for g in re.findall(r'fileID: 2100000, guid: (\w+)', text) if g in guids})
        print(relative, '\n  ' + '\n  '.join(materials))
    print('Synchronized', copied, 'files; snapshot contains', len(manifest), 'hashes')


if __name__ == '__main__':
    main()
