"""Preserve the approved PBR pass before refining the arena."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/Basement-arena-horror/before'
TARGETS = ['Assets/prefabs/Scenario/Basement.prefab', 'Assets/Art/Basement/BasementFinalArena.prefab',
           'Assets/TotalChaos/Runtime/TCFlickerLight.cs']


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    assert not (BEFORE / 'manifest.json').exists(), 'Do not overwrite the snapshot'
    manifest = {}
    for folder in ['Assets', 'Packages', 'ProjectSettings']:
        for p in (ROOT / folder).rglob('*'):
            if not p.is_file():
                continue
            relative = p.relative_to(ROOT)
            sha = digest(p)
            manifest[relative.as_posix()] = sha
            destination = PREVIEW / relative
            if not destination.exists() or digest(destination) != sha:
                destination.parent.mkdir(parents=True, exist_ok=True)
                shutil.copy2(p, destination)
    for name in TARGETS:
        p = BEFORE / name
        p.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(ROOT / name, p)
    (BEFORE / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    print('Arena snapshot saved;', len(manifest), 'hashes; isolated project synchronized.')


if __name__ == '__main__':
    main()
