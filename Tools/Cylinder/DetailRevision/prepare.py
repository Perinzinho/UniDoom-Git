"""Prepare an isolated Unity project and preserve the exact incoming asset state."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/CylinderDetailPreview'
BACKUP = ROOT / '.utmp/CylinderDetailBefore'


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    if WORK.exists() or BACKUP.exists():
        raise SystemExit('Existing revision workspace: refusing to overwrite it')
    BACKUP.mkdir(parents=True)
    manifest = {p.relative_to(ROOT).as_posix(): digest(p)
                for p in (ROOT / 'Assets').rglob('*') if p.is_file()}
    (BACKUP / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    prefab = 'Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab'
    shutil.copy2(ROOT / prefab, BACKUP / 'Cylinder.prefab')
    shutil.copy2(ROOT / (prefab + '.meta'), BACKUP / 'Cylinder.prefab.meta')
    for folder in ('Assets', 'Packages', 'ProjectSettings'):
        shutil.copytree(ROOT / folder, WORK / folder)
    print(f'Prepared isolated project. Preserved {len(manifest)} asset files.')


if __name__ == '__main__':
    main()
