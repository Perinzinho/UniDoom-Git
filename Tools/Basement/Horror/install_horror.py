"""Stage visual additions while retaining every existing object ID and gameplay block."""
import argparse
import hashlib
import importlib.util
import json
import re
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
PREVIEW = ROOT / '.utmp/BasementPreview'
BEFORE = ROOT / '.utmp/Basement-arena-horror/before'
ART = 'Assets/Art/Basement/ArenaHorror'
TARGETS = ['Assets/prefabs/Scenario/Basement.prefab', 'Assets/Art/Basement/BasementFinalArena.prefab']
RUNTIME = 'Assets/TotalChaos/Runtime/TCFlickerLight.cs'
CONVERTER = 'Assets/TotalChaos/Editor/TotalChaosMaterialConverter.cs'
spec = importlib.util.spec_from_file_location('pbr_installer', ROOT / 'Tools/Basement/Surface/install_surface.py')
base = importlib.util.module_from_spec(spec)
spec.loader.exec_module(base)


def stage():
    for path in TARGETS:
        original = (BEFORE / path).read_text(encoding='utf-8-sig')
        generated = (PREVIEW / path).read_text(encoding='utf-8-sig')
        old, new = base.blocks(original), base.blocks(generated)
        assert old.keys() <= new.keys(), 'Existing IDs removed'
        result = original[:original.index('--- !u!')]
        changed = 0
        for fid, (kind, block) in old.items():
            initial = block
            if kind == 108:
                block = new[fid][1]
            elif kind == 114 and base.script(block) in base.VISUAL_SCRIPTS:
                block = new[fid][1]
            elif kind == 23:
                for name in ['m_Enabled', 'm_Materials']:
                    block = base.substitute(block, new[fid][1], name)
            elif kind == 215:
                block = base.substitute(block, new[fid][1], 'm_CustomBakedTexture')
            elif kind == 4 and base.field(block, 'm_Children'):
                a, b = base.field(block, 'm_Children')[0], base.field(new[fid][1], 'm_Children')[0]
                old_ids, new_ids = re.findall(r'fileID: (-?\d+)', a), re.findall(r'fileID: (-?\d+)', b)
                assert new_ids[:len(old_ids)] == old_ids
                assert all(k not in old and new[k][0] == 4 for k in new_ids[len(old_ids):])
                block = base.substitute(block, new[fid][1], 'm_Children')
            elif kind == 1 and base.field(block, 'm_Component'):
                a, b = base.field(block, 'm_Component')[0], base.field(new[fid][1], 'm_Component')[0]
                old_ids, new_ids = re.findall(r'fileID: (-?\d+)', a), re.findall(r'fileID: (-?\d+)', b)
                assert new_ids[:len(old_ids)] == old_ids
                assert all(k not in old and new[k][0] == 114 and base.script(new[k][1]) == '207a0c2d979225248a336fcc408c7512' for k in new_ids[len(old_ids):])
                block = base.substitute(block, new[fid][1], 'm_Component')
            result += block
            changed += initial != block
        for fid, (kind, block) in new.items():
            if fid in old:
                continue
            assert kind in [1, 4, 23, 33, 205] or (kind == 114 and base.script(block) == '207a0c2d979225248a336fcc408c7512'), 'New solid collision/gameplay component'
            result += block
        staged = base.blocks(result)
        for fid, (kind, block) in old.items():
            if kind == 4:
                assert base.substitute(staged[fid][1], block, 'm_Children') == block
            if kind in [33, 65, 64, 54, 136, 1001] or (kind == 114 and base.script(block) not in base.VISUAL_SCRIPTS):
                assert staged[fid][1] == block, 'Original geometry/collision/gameplay changed'
        (PREVIEW / path).write_bytes(result.encode('utf-8'))
        print(path, len(old), 'IDs retained;', changed, 'visual blocks changed;', len(new)-len(old), 'decorative blocks added')


def install():
    base.require_log('Basement-arena-horror-final.log', ['BASEMENT_ARENA_HORROR_FINAL_OK', 'BASEMENT_SURFACE_GAMEPLAY_OK',
        'BASEMENT_ENCOUNTER_TEST_OK', 'BASEMENT_SURFACE_SRP_BATCHER_OK', 'Exiting batchmode successfully'])
    base.require_log('Basement-arena-horror-play.log', ['BASEMENT_SURFACE_PLAYMODE_OK', 'BASEMENT_ARENA_FLICKER_RUNTIME_OK'])
    manifest = json.loads((BEFORE / 'manifest.json').read_text())
    for name, expected in manifest.items():
        if name in [RUNTIME, CONVERTER]:
            assert (ROOT / name).read_bytes() == (PREVIEW / name).read_bytes()
        else:
            assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, 'Concurrent modification: ' + name
    for path in TARGETS:
        shutil.copy2(PREVIEW / path, ROOT / path)
    for path in (PREVIEW / ART).rglob('*'):
        if path.is_file():
            destination = ROOT / path.relative_to(PREVIEW)
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(path, destination)
    for path in [PREVIEW / ART] + list((PREVIEW / ART).rglob('*')):
        meta = Path(str(path)+'.meta')
        if path.is_dir() and meta.exists():
            shutil.copy2(meta, Path(str(ROOT / path.relative_to(PREVIEW))+'.meta'))
    for path in (PREVIEW / 'Preview/BasementSurface').glob('Horror-*.png'):
        shutil.copy2(path, ROOT / 'Tools/Basement/Horror' / path.name)
    for name in ['runtime-validation.txt', 'arena-flicker-validation.txt']:
        source = PREVIEW / 'Preview/BasementSurface' / name
        if source.exists():
            shutil.copy2(source, ROOT / 'Tools/Basement/Horror' / name)
    metrics = {}
    for line in (PREVIEW / 'Preview/BasementSurface/capture-metrics.txt').read_text().splitlines():
        if line.startswith('Horror-'):
            metrics[line.split(':')[0]] = line
    (ROOT / 'Tools/Basement/Horror/capture-metrics.txt').write_text('\n'.join(metrics.values())+'\n')
    print('Arena refinement installed. Existing solid collision, boss room, main scene and encounter unchanged.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    install() if args.install else stage()
