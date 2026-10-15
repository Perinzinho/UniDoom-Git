"""Merge only intended prefab fields; install after Unity validation and concurrency checks."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/CylinderDetailPreview'
BACKUP = ROOT / '.utmp/CylinderDetailBefore'
PREFAB = 'Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab'
SCENE = 'Assets/_Project/Scenes/Cylinder/'
BLOCK = re.compile(r'(?ms)^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)')


def blocks(text):
    return {m.group(2): (m.group(1), m.group(0)) for m in BLOCK.finditer(text)}


def field(block, name):
    match = re.search(r'(?m)^  ' + re.escape(name) + r':[^\n]*(?:\n(?:  -[^\n]*|    [^\n]*))*\n?', block)
    if not match:
        raise ValueError('Missing field ' + name)
    return match.group(0)


def replace_field(target, source, name):
    return target.replace(field(target, name), field(source, name), 1)


def stage():
    before = (BACKUP / 'Cylinder.prefab').read_text(encoding='utf-8-sig')
    generated = (WORK / PREFAB).read_text(encoding='utf-8-sig')
    if not (BACKUP / 'generated.prefab').exists():
        (BACKUP / 'generated.prefab').write_text(generated, encoding='utf-8')
    old, new = blocks(before), blocks(generated)
    if not old.keys() <= new.keys():
        raise ValueError('Original fileIDs removed')
    changed = []
    merged = {}
    for identifier, (kind, original) in old.items():
        saved = new[identifier][1]
        result = original
        if kind == '4':
            result = replace_field(result, saved, 'm_Children')
            # Only pre-existing light transforms may change orientation.
            go = re.search(r'm_GameObject: \{fileID: (-?\d+)', original).group(1)
            if any(t == '108' and f'm_GameObject: {{fileID: {go}}}' in b for t, b in old.values()):
                result = replace_field(result, saved, 'm_LocalRotation')
                result = replace_field(result, saved, 'm_LocalEulerAnglesHint')
        elif kind == '23':
            for name in ('m_Materials', 'm_Enabled'):
                result = replace_field(result, saved, name)
        elif kind == '108':
            result = saved
        elif kind == '114':
            if 'fogDensity:' in original or 'sharedProfile:' in original:
                result = saved
            elif 'm_Enabled:' in original:
                # This includes the intentional removal of random lamp outages.
                result = replace_field(result, saved, 'm_Enabled')
        if result != original:
            changed.append(dict(fileID=identifier, type=kind))
        merged[identifier] = result
    result = before[:before.index('--- !u!')]
    result += ''.join(merged.get(m.group(2), m.group(0)) for m in BLOCK.finditer(generated))
    (WORK / PREFAB).write_text(result, encoding='utf-8', newline='\n')
    report = dict(original_fileIDs=len(old), new_fileIDs=len(new) - len(old), modified_original_blocks=changed,
                  original_colliders_preserved=all(merged[i] == b for i, (t, b) in old.items() if t in ('64', '65', '135', '136')),
                  original_mesh_filters_preserved=all(merged[i] == b for i, (t, b) in old.items() if t == '33'))
    (ROOT / 'Tools/Cylinder/DetailRevision/structure-validation.json').write_text(json.dumps(report, indent=2))
    print(f'Staged {len(old)} original IDs and {len(new)-len(old)} additions; original collision and mesh bindings preserved.')


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def install():
    evidence = WORK / 'Evidence'
    for name in ('edit-validation.txt', 'play-validation.txt'):
        if 'PASS' not in (evidence / name).read_text():
            raise ValueError('Validation missing: ' + name)
    before = json.loads((BACKUP / 'manifest.json').read_text())
    changes = []
    for path in (WORK / 'Assets').rglob('*'):
        if not path.is_file():
            continue
        rel = path.relative_to(WORK).as_posix()
        allowed = (rel == PREFAB or (rel.startswith(SCENE) and '/DetailRevision' in rel)
                   or rel.startswith(SCENE + 'Scripts/CylinderEnemySpawns.cs') or rel == SCENE + 'Scripts.meta')
        if not allowed or (rel in before and before[rel] == digest(path)):
            continue
        target = ROOT / rel
        if rel in before:
            if not target.exists() or digest(target) != before[rel]:
                raise ValueError('Concurrent asset change: ' + rel)
        elif target.exists():
            raise ValueError('Unexpected existing output: ' + rel)
        changes.append((path, target, rel))
    for source, target, rel in changes:
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            backup = BACKUP / 'originals' / rel
            backup.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(target, backup)
        shutil.copy2(source, target)
    for source in evidence.glob('*'):
        if source.is_file():
            shutil.copy2(source, ROOT / 'Tools/Cylinder/DetailRevision' / source.name)
    after = {p.relative_to(ROOT).as_posix(): digest(p) for p in (ROOT / 'Assets').rglob('*') if p.is_file()}
    unrelated = [rel for rel, value in before.items() if rel != PREFAB and (rel not in after or after[rel] != value)]
    record = dict(installed=[dict(path=rel, sha256=digest(target)) for _, target, rel in changes],
                  unrelated_existing_assets_changed=unrelated)
    (ROOT / 'Tools/Cylinder/DetailRevision/installed.json').write_text(json.dumps(record, indent=2))
    print(f'Installed {len(changes)} files. Other incoming assets changed: {len(unrelated)}')


def restore_prefab():
    record = json.loads((ROOT / 'Tools/Cylinder/DetailRevision/installed.json').read_text())
    expected = next(item['sha256'] for item in record['installed'] if item['path'] == PREFAB)
    if digest(ROOT / PREFAB) != expected:
        raise ValueError('Prefab has subsequent changes; refusing to overwrite them')
    shutil.copy2(BACKUP / 'Cylinder.prefab', ROOT / PREFAB)
    print('Incoming prefab restored. New assets retained for regeneration; no files deleted.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser()
    parser.add_argument('--install', action='store_true')
    parser.add_argument('--restore-prefab', action='store_true')
    args = parser.parse_args()
    restore_prefab() if args.restore_prefab else install() if args.install else stage()
