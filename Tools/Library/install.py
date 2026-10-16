"""Stage or install the library while retaining existing YAML identities and user data."""
import argparse
import hashlib
import json
import os
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / '.utmp/LibraryPreview'
ART = Path('Assets/_Project/Scenes/Library')
PREFAB = ART / 'Prefabs/Library.prefab'
EVIDENCE = ROOT / 'Tools/Library'

def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in
            re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}

def preserve(original, generated):
    old, new = blocks(original), blocks(generated)
    assert set(old).issubset(new), 'Original Unity object IDs were removed'
    allowed = {4: ['m_Children', 'm_LocalScale'], 23: ['m_Materials'],
               33: ['m_Mesh'], 64: ['m_Mesh'], 114: ['m_Mesh']}
    result = original[:original.index('--- !u!')]
    changes = []
    for fid, (kind, block) in old.items():
        assert kind == new[fid][0]
        for field in allowed.get(kind, []):
            if kind == 4 and fid != '1420356484721759852':
                continue
            pattern = rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
            a = re.search(pattern, block, re.M | re.S)
            b = re.search(pattern, new[fid][1], re.M | re.S)
            if a and b and a[0] != b[0]:
                block = block[:a.start()] + b[0] + block[a.end():]
                changes.append({'fileID': fid, 'type': kind, 'field': field})
        result += block
    result += ''.join(re.sub(r'[ \t]+$', '', block, flags=re.M)
                      for fid, (_, block) in new.items() if fid not in old)
    return result.encode('utf-8'), dict(preserved_ids=len(old), added_ids=len(new)-len(old), changes=changes)

def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    baseline = json.loads((EVIDENCE / 'before-manifest.json').read_text())
    for relative in (PREFAB.as_posix(), PREFAB.as_posix()+'.meta'):
        assert digest(ROOT / relative) == baseline[relative], 'Concurrent edit: ' + relative
    log = (WORK / ('final.log' if args.install else 'build.log')).read_text(errors='replace')
    completed = 'Exiting batchmode successfully' in log
    if args.install:
        completed = completed or ('LIBRARY_PLAY_PASS' in log and
                    (WORK / 'Evidence/play-validation.txt').read_text().startswith('PASS\n'))
    assert 'LIBRARY_VERIFY_PASS' in log and completed, 'Unity verification incomplete'
    assert 'error CS' not in log and '\nException:' not in log
    original = (EVIDENCE / 'Before/Library.prefab').read_text(encoding='utf-8')
    generated = (WORK / PREFAB).read_text(encoding='utf-8')
    data, report = preserve(original, generated)
    if not args.install:
        (WORK / PREFAB).write_bytes(data)
        (EVIDENCE / 'structure-validation.json').write_text(json.dumps(report, indent=2))
        print('Staged with all original fileIDs preserved:', report['preserved_ids'])
        return
    assert (WORK / PREFAB).read_bytes() == data, 'Prefab differs from staged verification'
    installed = []
    dependencies = set((WORK / 'Evidence/dependencies.txt').read_text().splitlines())
    for source in (WORK / ART).rglob('*'):
        if not source.is_file():
            continue
        relative = source.relative_to(WORK)
        if relative == PREFAB:
            continue
        base = relative.as_posix().removesuffix('.meta')
        if ('/Meshes/' in base or '/Textures/PackedMasks/' in base) and base not in dependencies:
            continue
        target = ROOT / relative
        if target.exists():
            if relative != PREFAB:
                assert digest(target) == digest(source), 'Unexpected overwrite: ' + str(relative)
                continue
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        installed.append(dict(path=relative.as_posix(), sha256=digest(target)))
    # Importable dependencies are present before the original prefab is atomically replaced.
    assert digest(ROOT / PREFAB) == baseline[PREFAB.as_posix()], 'Concurrent prefab edit during installation'
    staged = ROOT / '.utmp/Library-install.prefab'
    staged.write_bytes(data)
    os.replace(staged, ROOT / PREFAB)
    installed.append(dict(path=PREFAB.as_posix(), sha256=digest(ROOT / PREFAB)))
    for source in (WORK / 'Evidence').iterdir():
        if source.is_file():
            shutil.copy2(source, EVIDENCE / source.name)
    (EVIDENCE / 'installed.json').write_text(json.dumps(installed, indent=2))
    print('Installed', len(installed), 'files; main scene and other assets were not overwritten.')

if __name__ == '__main__':
    main()
