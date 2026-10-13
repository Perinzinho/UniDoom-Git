"""Install the verified Cylinder while preserving original ProBuilder data and IDs."""
import re
import shutil
import argparse
import subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
PREVIEW = ROOT / '.utmp/BasementPreview'
PREFAB = Path('Assets/prefabs/Scenario/Cylinder.prefab')


def blocks(text):
    return {int(m[2]): (int(m[1]), m[0]) for m in
            re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def field(text, name):
    m = re.search(rf'  {name}:[^\n]*(?:\n  -[^\n]*)*', text)
    if not m:
        raise ValueError(name)
    return m[0]


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--replace', action='store_true', help='Replace the prior Cylinder after checking its saved snapshot')
    parser.add_argument('--snapshot', default='.utmp/Cylinder-before-style.prefab', help='Expected current prefab, relative to workspace')
    args = parser.parse_args()
    log = (ROOT / '.utmp/Cylinder-build.log').read_text(encoding='utf-8-sig')
    assert all(marker in log for marker in ['CYLINDER_BUILD_OK', 'CYLINDER_PLAYER_ROUTES_OK',
               'CYLINDER_CANTEEN_SUPPORT_OK', 'CYLINDER_ROOF_COVERAGE_OK', 'CYLINDER_DOORS_OK']), 'Unity validation must pass'
    original = (ROOT / '.utmp/Cylinder-original.prefab').read_text(encoding='utf-8-sig')
    snapshot = (ROOT / args.snapshot).resolve()
    assert snapshot.is_relative_to(ROOT.resolve()), 'Snapshot must remain within workspace'
    expected = snapshot.read_text(encoding='utf-8-sig') if args.replace else original
    assert (ROOT / PREFAB).read_text(encoding='utf-8-sig') == expected, 'Source changed during generation'
    generated = (PREVIEW / PREFAB).read_text(encoding='utf-8-sig')
    old, new = blocks(original), blocks(generated)
    result = original[:original.index('--- !u!')]
    for fid, (kind, b) in old.items():
        assert fid in new and kind == new[fid][0], f'Lost original object {fid}'
        rebuilt = new[fid][1]
        names = {1: ['m_IsActive'], 4: ['m_Children'], 23: ['m_Materials'],
                 33: ['m_Mesh'], 64: ['m_Mesh'], 114: ['m_Mesh']}.get(kind, [])
        for name in names:
            if f'  {name}:' in b and f'  {name}:' in rebuilt:
                b = b.replace(field(b, name), field(rebuilt, name))
        result += b
    result += ''.join(re.sub(r'[ \t]+(?=\n)', '', b) for fid, (_, b) in new.items() if fid not in old)
    art = Path('Assets/Art/Cylinder')
    # Remove only obsolete generated files under the verified Cylinder folder.
    destination = (ROOT / art).resolve()
    assert destination == ROOT.resolve() / art
    new_files = {p.relative_to(PREVIEW / art) for p in (PREVIEW / art).rglob('*') if p.is_file()}
    if destination.exists():
        for p in destination.rglob('*'):
            if p.is_file() and p.relative_to(destination) not in new_files:
                p.unlink()
    shutil.copytree(PREVIEW / art, ROOT / art, dirs_exist_ok=True)
    shutil.copy2(PREVIEW / (str(art)+'.meta'), ROOT / (str(art)+'.meta'))
    (ROOT / PREFAB).write_text(result, encoding='utf-8', newline='\n')
    referenced = set(re.findall(r'guid: (\w+)', result))
    for scene in (ROOT / art).glob('*.unity'):
        referenced.update(re.findall(r'guid: (\w+)', scene.read_text(encoding='utf-8-sig')))
    for p in (ROOT / art / 'Meshes').glob('*.asset'):
        meta = Path(str(p)+'.meta')
        guid = re.search(r'^guid: (\w+)', meta.read_text(encoding='utf-8-sig'), re.M)[1]
        if guid not in referenced:
            p.unlink()
            meta.unlink()
    for f in (PREVIEW / 'Preview').glob('Cylinder-*.png'):
        shutil.copy2(f, ROOT / 'Tools/Cylinder' / f.name)
    # Check that every generated asset reference resolves in the actual workspace.
    required = set()
    for p in [ROOT / PREFAB, *(ROOT / art).rglob('*.mat'), *(ROOT / art).rglob('*.unity')]:
        for guid in re.findall(r'guid: (\w+)', p.read_text(encoding='utf-8-sig')):
            if not guid.startswith('0000000000000000'):
                required.add(guid)
    found = subprocess.run(['rg', '-l', '--no-ignore', '-g', '*.meta',
                            '^guid: ('+'|'.join(sorted(required))+')\r?$',
                            str(ROOT / 'Assets'), str(PREVIEW / 'Library/PackageCache')],
                           capture_output=True, text=True, encoding='utf-8')
    guids = set()
    for name in found.stdout.splitlines():
        m = re.search(r'^guid: (\w+)', Path(name).read_text(encoding='utf-8-sig'), re.M)
        if m:
            guids.add(m[1])
    missing = required-guids
    print(f'Installed Cylinder: preserved {len(old)} original blocks; added {len(new)-len(old)} blocks')
    assert not missing, f'Unresolved references: {sorted(missing)}'
    print('All prefab, material and preview scene asset references resolve')


if __name__ == '__main__':
    main()
