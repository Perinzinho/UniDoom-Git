"""Integrate reviewed visual fields, retaining all incoming IDs, physics and gameplay."""
import argparse
import hashlib
import importlib.util
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/CylinderDetailPreview'
BACKUP = ROOT / '.utmp/CylinderCanteenDoorsBefore'
PREFAB = 'Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab'
spec = importlib.util.spec_from_file_location('detail_merge', ROOT / 'Tools/Cylinder/DetailRevision/install.py')
merge = importlib.util.module_from_spec(spec)
spec.loader.exec_module(merge)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def stage():
    before = (BACKUP / 'Cylinder.prefab').read_text(encoding='utf-8-sig')
    generated = (WORK / PREFAB).read_text(encoding='utf-8-sig')
    (BACKUP / 'generated.prefab').write_text(generated, encoding='utf-8')
    old, new = merge.blocks(before), merge.blocks(generated)
    if not old.keys() <= new.keys():
        raise ValueError('An existing object/component was removed')
    combined = {}
    modified = []
    for identifier, (kind, original) in old.items():
        result = original
        saved = new[identifier][1]
        if kind == '4':
            result = merge.replace_field(result, saved, 'm_Children')
        elif kind == '23':
            for field in ('m_Materials', 'm_Enabled'):
                result = merge.replace_field(result, saved, field)
        elif kind == '102':
            result = merge.replace_field(result, saved, 'm_Text')
        # Every other existing component remains byte-for-byte unchanged.
        if result != original:
            modified.append(dict(fileID=identifier, type=kind))
        combined[identifier] = result
    output = before[:before.index('--- !u!')]
    output += ''.join(combined.get(m.group(2), m.group(0)) for m in merge.BLOCK.finditer(generated))
    (WORK / PREFAB).write_text(output, encoding='utf-8', newline='\n')
    report = dict(original_fileIDs=len(old), new_fileIDs=len(new)-len(old), modified_existing_blocks=modified,
                  existing_colliders_preserved=all(combined[i] == b for i, (t, b) in old.items() if t in ('64','65','135','136')),
                  existing_gameplay_scripts_preserved=all(combined[i] == b for i, (t, b) in old.items() if t == '114'),
                  existing_mesh_bindings_preserved=all(combined[i] == b for i, (t, b) in old.items() if t == '33'))
    (ROOT / 'Tools/Cylinder/CanteenDoors/structure-validation.json').write_text(json.dumps(report, indent=2))
    print(f'Staged: {len(old)} existing IDs preserved; {len(new)-len(old)} new blocks.')


def install():
    evidence = WORK / 'CanteenEvidence'
    for name in ('edit-validation.txt', 'play-validation.txt'):
        if not (evidence / name).exists() or not (evidence / name).read_text().startswith('PASS\n'):
            raise ValueError('Required validation missing: ' + name)
    original = json.loads((BACKUP / 'manifest.json').read_text())
    files = []
    for source in (WORK / 'Assets/_Project/Scenes/Cylinder').rglob('*'):
        if not source.is_file():
            continue
        rel = source.relative_to(WORK).as_posix()
        if rel != PREFAB and '/CanteenDoors' not in rel:
            continue
        if rel in original and sha(source) == original[rel]:
            continue
        target = ROOT / rel
        if rel in original:
            if not target.exists() or sha(target) != original[rel]:
                raise ValueError('Concurrent change: ' + rel)
        elif target.exists():
            raise ValueError('Output already exists: ' + rel)
        files.append((source, target, rel))
    for source, target, rel in files:
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
    for source in evidence.iterdir():
        if source.is_file():
            shutil.copy2(source, ROOT / 'Tools/Cylinder/CanteenDoors' / source.name)
    changed_other = [rel for rel, digest in original.items() if rel != PREFAB and (not (ROOT / rel).exists() or sha(ROOT / rel) != digest)]
    report = dict(installed=[dict(path=rel, sha256=sha(target)) for _,target,rel in files],
                  other_existing_assets_changed=changed_other)
    (ROOT / 'Tools/Cylinder/CanteenDoors/installed.json').write_text(json.dumps(report, indent=2))
    print(f'Installed {len(files)} files; other existing assets changed: {len(changed_other)}')


def restore():
    report = json.loads((ROOT / 'Tools/Cylinder/CanteenDoors/installed.json').read_text())
    expected = next(item['sha256'] for item in report['installed'] if item['path'] == PREFAB)
    if sha(ROOT / PREFAB) != expected:
        raise ValueError('Refusing to overwrite subsequent prefab changes')
    shutil.copy2(BACKUP / 'Cylinder.prefab', ROOT / PREFAB)
    print('Incoming prefab restored; new assets retained. No deletion.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--install', action='store_true')
    parser.add_argument('--restore-prefab', action='store_true')
    args = parser.parse_args()
    restore() if args.restore_prefab else install() if args.install else stage()
