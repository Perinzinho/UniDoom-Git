"""Verify the installed artifact, original fileIDs and the untouched project baseline."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Tools/Library'
PREFAB = 'Assets/_Project/Scenes/Library/Prefabs/Library.prefab'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

if __name__ == '__main__':
    baseline = json.loads((OUT / 'before-manifest.json').read_text())
    installed = json.loads((OUT / 'installed.json').read_text())
    mismatches = [item['path'] for item in installed if digest(ROOT / item['path']) != item['sha256']]
    assert not mismatches, mismatches
    changed = [p for p, h in baseline.items() if not (ROOT / p).is_file() or digest(ROOT / p) != h]
    assert changed == [PREFAB], changed
    original = (OUT / 'Before/Library.prefab').read_text(encoding='utf-8')
    current = (ROOT / PREFAB).read_text(encoding='utf-8')
    ids = lambda text: set(re.findall(r'^--- !u!\d+ &(-?\d+)', text, re.M))
    assert ids(original).issubset(ids(current))
    guids = {}
    duplicates = []
    for path in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})', path.read_text(errors='replace'), re.M)
        if match:
            guid = match[1]
            if guid in guids:
                duplicates.append([str(guids[guid]), str(path)])
            guids[guid] = path
    assert not duplicates, duplicates
    missing = [g for g in set(re.findall(r'guid: ([0-9a-f]{32})', current))
               if g not in guids and not g.startswith('0000000000000000')]
    # Package script GUIDs live in the package cache, outside Assets.
    for path in (ROOT / '.utmp/LibraryPreview/Library/PackageCache').rglob('*.meta'):
        if not missing:
            break
        match = re.search(r'^guid: ([0-9a-f]{32})', path.read_text(errors='replace'), re.M)
        if match and match[1] in missing:
            missing.remove(match[1])
    assert not missing, missing
    result = dict(status='PASS', installed_files=len(installed), baseline_files=len(baseline),
                  changed_original_files=changed, preserved_original_fileIDs=len(ids(original)),
                  duplicate_GUIDs=0, unresolved_prefab_GUIDs=0,
                  sample_scene_byte_identical=True, other_original_assets_and_settings_byte_identical=True)
    (OUT / 'installed-validation.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result))
