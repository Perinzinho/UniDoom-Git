"""Retain IDs and gameplay YAML; install only the validated Basement visual pass."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/Basement-PBR/before'
ART = 'Assets/Art/Basement/Surface'
TARGETS = ['Assets/prefabs/Scenario/Basement.prefab', 'Assets/Art/Basement/BasementFinalArena.prefab',
           'Assets/Art/Basement/BasementFinalSanctum.prefab']
CONVERTER = 'Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs'
RUNTIME = 'Assets/TotalChaos/Runtime/TCRegionAtmosphere.cs'
VISUAL_SCRIPTS = {'e9bd2423b59bca04ea3b4080e5132864', '172515602e62fb746b5d573b38a5fe58',
                  '207a0c2d979225248a336fcc408c7512', '474bcb49853aa07438625e644c072ee6'}


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def field(text, name):
    return re.search(rf'^  {name}:.*?(?=^  [A-Za-z_]|\Z)', text, re.M | re.S)


def script(block):
    match = re.search(r'm_Script:.*guid: (\w+)', block)
    return match[1] if match else ''


def substitute(original, generated, name):
    a, b = field(original, name), field(generated, name)
    assert a and b, name
    return original[:a.start()] + b[0] + original[a.end():]


def stage():
    for path in TARGETS:
        original = (BEFORE / path).read_text(encoding='utf-8-sig')
        generated = (PREVIEW / path).read_text(encoding='utf-8-sig')
        old, new = blocks(original), blocks(generated)
        assert old.keys() <= new.keys(), 'Existing ID removed: ' + path
        lights = {re.search(r'm_GameObject: \{fileID: (\d+)', block)[1]
                  for kind, block in old.values() if kind == 108}
        regions = {re.search(r'm_GameObject: \{fileID: (\d+)', block)[1]
                   for kind, block in old.values() if kind == 114 and script(block) == 'e9bd2423b59bca04ea3b4080e5132864'}
        result = original[:original.index('--- !u!')]
        changed = 0
        for fid, (kind, block) in old.items():
            initial = block
            if kind == 23:
                block = substitute(block, new[fid][1], 'm_Materials')
            elif kind == 108 or kind == 114 and script(block) in VISUAL_SCRIPTS:
                block = new[fid][1]
            elif kind == 65 and re.search(r'm_GameObject: \{fileID: (\d+)', block)[1] in regions:
                assert 'm_IsTrigger: 1' in block and 'm_IsTrigger: 1' in new[fid][1]
                for name in ['m_Center', 'm_Size']:
                    block = substitute(block, new[fid][1], name)
            elif kind == 4 and 'm_GameObject:' in block:
                go = re.search(r'm_GameObject: \{fileID: (\d+)', block)[1]
                if go in lights:
                    for name in ['m_LocalRotation', 'm_LocalEulerAnglesHint']:
                        block = substitute(block, new[fid][1], name)
                children, original_children = field(new[fid][1], 'm_Children'), field(block, 'm_Children')
                if children and original_children and children[0] != original_children[0]:
                    old_ids = re.findall(r'fileID: (-?\d+)', original_children[0])
                    new_ids = re.findall(r'fileID: (-?\d+)', children[0])
                    assert new_ids[:len(old_ids)] == old_ids, 'Existing children changed'
                    assert all(k not in old and new[k][0] == 4 for k in new_ids[len(old_ids):])
                    block = substitute(block, new[fid][1], 'm_Children')
            elif kind == 1:
                components = field(new[fid][1], 'm_Component')
                original_components = field(block, 'm_Component')
                if components and original_components and components[0] != original_components[0]:
                    assert fid in lights, 'Components added to non-light object'
                    old_ids = set(re.findall(r'fileID: (-?\d+)', original_components[0]))
                    added = set(re.findall(r'fileID: (-?\d+)', components[0])) - old_ids
                    assert old_ids <= set(re.findall(r'fileID: (-?\d+)', components[0]))
                    assert all(new[k][0] == 114 and script(new[k][1]) in VISUAL_SCRIPTS for k in added)
                    block = substitute(block, new[fid][1], 'm_Component')
            changed += initial != block
            result += block
        for fid, (kind, block) in new.items():
            if fid in old:
                continue
            assert (kind == 114 and script(block) in VISUAL_SCRIPTS) or kind in (1, 4, 215), 'Unexpected new component'
            if kind == 1:
                assert 'm_Name: Basement static reflection' in block, 'Unexpected new object'
            result += block
        (PREVIEW / path).write_bytes(result.encode('utf-8'))
        print(path, len(old), 'IDs retained;', changed, 'visual blocks changed')


def require_log(name, markers):
    text = (ROOT / '.utmp' / name).read_text(encoding='utf-8', errors='replace')
    for marker in markers:
        assert marker in text, name + ': missing ' + marker
    for error in ['error CS', 'Exception:', 'Shader error in', 'Render Graph Execution error']:
        assert error not in text, name + ': ' + error


def install():
    require_log('Basement-PBR-final.log', ['BASEMENT_SURFACE_FINAL_OK', 'BASEMENT_SURFACE_GAMEPLAY_OK',
                                        'BASEMENT_SURFACE_REVERSAL_OK', 'BASEMENT_SURFACE_SRP_BATCHER_OK',
                                        'BASEMENT_ENCOUNTER_TEST_OK', 'Exiting batchmode successfully'])
    require_log('Basement-PBR-play.log', ['BASEMENT_SURFACE_PLAYMODE_OK'])
    manifest = json.loads((BEFORE / 'manifest.json').read_text())
    for name, expected in manifest.items():
        if name in [CONVERTER, RUNTIME]:
            assert (ROOT / name).read_bytes() == (PREVIEW / name).read_bytes()
        else:
            assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, 'Concurrent change: ' + name
    # Every staged block may differ only in renderer, light, or known atmosphere data.
    for path in TARGETS:
        old = blocks((BEFORE / path).read_text(encoding='utf-8-sig'))
        new = blocks((PREVIEW / path).read_text(encoding='utf-8-sig'))
        assert old.keys() <= new.keys()
        for fid, (kind, block) in old.items():
            modified = new[fid][1]
            if kind == 23:
                assert substitute(modified, block, 'm_Materials') == block
            elif kind == 4:
                for name in ['m_LocalRotation', 'm_LocalEulerAnglesHint', 'm_Children']:
                    if field(block, name):
                        modified = substitute(modified, block, name)
                assert modified == block, 'Transform changed: ' + fid
            elif kind == 1:
                if field(block, 'm_Component'):
                    modified = substitute(modified, block, 'm_Component')
                assert modified == block
            elif kind == 65 and 'm_IsTrigger: 1' in block:
                for name in ['m_Center', 'm_Size']:
                    modified = substitute(modified, block, name)
                assert modified == block
            elif kind != 108 and not (kind == 114 and script(block) in VISUAL_SCRIPTS):
                assert modified == block, 'Gameplay/mesh/collision changed: ' + fid
        shutil.copy2(PREVIEW / path, ROOT / path)
    for path in (PREVIEW / ART).rglob('*'):
        if path.is_file():
            target = ROOT / path.relative_to(PREVIEW)
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, target)
    for path in [PREVIEW / ART] + list((PREVIEW / ART).rglob('*')):
        meta = Path(str(path) + '.meta')
        if path.is_dir() and meta.exists():
            shutil.copy2(meta, Path(str(ROOT / path.relative_to(PREVIEW)) + '.meta'))
    captures = PREVIEW / 'Preview/BasementSurface'
    for path in captures.iterdir():
        if path.name == 'Before-Corridor.png':
            continue  # Old preview did not account for the scene's passage overrides.
        if path.name.startswith(('Before-', 'Final-')) or path.name in ['validation.txt', 'runtime-validation.txt', 'corridor-camera.txt']:
            shutil.copy2(path, ROOT / 'Tools/Basement/Surface' / path.name)
    metrics = {}
    for line in (captures / 'capture-metrics.txt').read_text().splitlines():
        if line.startswith(('Before-', 'Final-')) and not line.startswith('Before-Corridor:'):
            metrics[line.split(':')[0]] = line
    (ROOT / 'Tools/Basement/Surface/capture-metrics.txt').write_text('\n'.join(metrics.values()) + '\n')
    shutil.copy2(ROOT / RUNTIME, ROOT / 'Tools/TotalChaosSkill/Unity/Runtime/TCRegionAtmosphere.cs')
    print('Installed Basement visual pass. All prior IDs, meshes, collision and gameplay blocks retained.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    install() if args.install else stage()
