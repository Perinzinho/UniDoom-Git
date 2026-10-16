import concurrent.futures
import hashlib
import json
import os
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
OUT = ROOT / 'Tools/Library/EncounterRevision'
WORK = ROOT / '.utmp/LibraryPreview'
ART = Path('Assets/_Project/Scenes/Library')
PREFAB = ART / 'Prefabs/Library.prefab'


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def main():
    before = json.loads((OUT / 'before-manifest.json').read_text())
    assert sha(ROOT / PREFAB) == before[PREFAB.as_posix()], 'Concurrent prefab edit'
    geometry = (OUT / 'geometry-validation.log').read_text(errors='replace').split('Reloading assemblies for play mode.')[0]
    play = (WORK / 'encounter-play.log').read_text(errors='replace')
    for marker in ['LIBRARY_ENCOUNTER_BUILD_PASS', 'LIBRARY_ENCOUNTER_VERIFY_PASS', 'LIBRARY_ENCOUNTER_MAP_PASS']:
        assert marker in geometry, marker
    for marker in ['LIBRARY_ENCOUNTER_BUILD_PASS', 'LIBRARY_ENCOUNTER_PLAY_PASS']:
        assert marker in play, marker
    assert 'error CS' not in geometry + play and 'Shader error' not in geometry + play
    for name in ['validation.txt', 'play-validation.txt']:
        assert (WORK / 'Evidence/Encounter' / name).read_text().startswith('PASS')

    old = blocks((OUT / 'Library.prefab').read_text(encoding='utf-8'))
    new = blocks((WORK / PREFAB).read_text(encoding='utf-8'))
    assert old.keys() <= new.keys(), 'Existing prefab fileIDs removed'
    shell = blocks((ROOT / 'Tools/Library/Before/Library.prefab').read_text(encoding='utf-8'))
    for fid, (kind, block) in old.items():
        assert new[fid][0] == kind
        if kind == 4 and fid in shell:
            for field in ['m_LocalPosition', 'm_LocalRotation', 'm_LocalScale', 'm_Father']:
                pattern = rf'^  {field}:.*$'
                assert re.search(pattern, block, re.M)[0] == re.search(pattern, new[fid][1], re.M)[0], (fid, field)

    def check(item):
        name, digest = item
        path = ROOT / name
        assert path.is_file(), 'Existing file removed: ' + name
        current = sha(path)
        return (name, current) if name != PREFAB.as_posix() and current != digest else None

    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:
        concurrent_changes = dict(x for x in pool.map(check, before.items()) if x)

    paths = {p for p in (WORK / ART).rglob('*') if p.is_file() and
             ('/EncounterRevision/' in p.as_posix() or p.name == 'EncounterRevision.meta')}
    for parent in ['Scripts', 'Navigation']:
        rel = ART / (parent + '.meta')
        if not (ROOT / rel).exists():
            assert (WORK / rel).is_file()
            paths.add(WORK / rel)
    # Metas first, then assets, then the owning prefab; retain every existing GUID.
    installed = []
    for path in sorted(paths, key=lambda p: (p.suffix != '.meta', str(p))):
        rel = path.relative_to(WORK)
        target = ROOT / rel
        target.parent.mkdir(parents=True, exist_ok=True)
        if target.exists():
            assert sha(target) == sha(path), 'Unexpected overwrite: ' + str(rel)
        else:
            shutil.copy2(path, target)
        installed.append(dict(path=rel.as_posix(), sha256=sha(target)))
    data = re.sub(r'[ \t]+$', '', (WORK / PREFAB).read_text(encoding='utf-8'), flags=re.M).encode('utf-8')
    temporary = ROOT / '.utmp/LibraryEncounter.prefab'
    temporary.write_bytes(data)
    os.replace(temporary, ROOT / PREFAB)
    installed.append(dict(path=PREFAB.as_posix(), sha256=sha(ROOT / PREFAB)))
    for path in (WORK / 'Evidence/Encounter').iterdir():
        if path.is_file():
            shutil.copy2(path, OUT / path.name)
    (OUT / 'geometry-validation.log').write_text(geometry, encoding='utf-8')
    shutil.copy2(WORK / 'encounter-play.log', OUT / 'play-validation.log')

    for item in installed:
        path = ROOT / item['path']
        assert sha(path) == item['sha256']
        if path.suffix != '.meta':
            assert Path(str(path) + '.meta').is_file()
    changed = []
    for name, digest in before.items():
        if name == PREFAB.as_posix():
            continue
        assert (ROOT / name).exists(), 'Existing file removed: ' + name
        current = sha(ROOT / name)
        if current != digest:
            assert concurrent_changes.get(name) == current, 'Concurrent change during install: ' + name
            changed.append(name)
    guids = {}
    for path in (ROOT / 'Assets').rglob('*.meta'):
        match = re.search(r'^guid: ([0-9a-f]{32})$', path.read_text(errors='replace'), re.M)
        if match:
            guids.setdefault(match[1], []).append(path.relative_to(ROOT).as_posix())
    added = {x['path'] for x in installed if x['path'] not in before}
    collisions = {g: p for g, p in guids.items() if len(p) > 1 and set(p) & added}
    assert not collisions, collisions
    result = dict(status='PASS', installed_files=len(installed), existing_fileIDs_preserved=len(old),
                  original_shell_transforms_preserved=True, new_guid_collisions=collisions,
                  concurrent_changes_preserved=changed,
                  main_scene_unchanged=sha(ROOT / 'Assets/_Project/Scenes/Main/SampleScene.unity') == before['Assets/_Project/Scenes/Main/SampleScene.unity'],
                  validation='Unity 6000.5.7f1 isolated project: prefab, actual main map, capsule clearance, NavMesh and Play Mode encounter traversal.')
    (OUT / 'installed.json').write_text(json.dumps(installed, indent=2))
    (OUT / 'installed-validation.json').write_text(json.dumps(result, indent=2))
    print(json.dumps(result, indent=2))


if __name__ == '__main__':
    main()
