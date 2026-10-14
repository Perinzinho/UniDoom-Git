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
SCENE = 'Assets/Scenes/SampleScene.unity'
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
            if kind == 4 and base.field(block, 'm_Children'):
                assert base.substitute(staged[fid][1], block, 'm_Children') == block
            if kind in [33, 65, 64, 54, 136, 1001] or (kind == 114 and base.script(block) not in base.VISUAL_SCRIPTS):
                assert staged[fid][1] == block, 'Original geometry/collision/gameplay changed'
        (PREVIEW / path).write_bytes(result.encode('utf-8'))
        print(path, len(old), 'IDs retained;', changed, 'visual blocks changed;', len(new)-len(old), 'decorative blocks added')


def install():
    build_marker = PREVIEW / 'Preview/ArenaHorror/dismemberment-build.txt'
    assert build_marker.exists() and build_marker.read_text().strip() == 'upper-thigh-remains-v2', \
        'Refusing stale staging: rebuild the arena after the dismemberment update'
    staged_arena = (PREVIEW / TARGETS[1]).read_text(encoding='utf-8-sig')
    for name in ['Dismembered casualties from pre-arena body', 'West headless torso missing left arm and right leg',
                 'East headless torso missing right arm and left leg']:
        assert name in staged_arena, 'Refusing stale staging: missing ' + name
    for name in ['Remains_Corpse_skin.asset', 'Remains_Severed_tissue.asset', 'Remains_Dried_seepage.asset']:
        assert (PREVIEW / ART / 'Meshes' / name).exists(), 'Refusing stale staging: missing ' + name
    base.require_log('Basement-arena-horror-final.log', ['BASEMENT_ARENA_HORROR_FINAL_OK', 'BASEMENT_SURFACE_GAMEPLAY_OK',
        'BASEMENT_ENCOUNTER_TEST_OK', 'BASEMENT_SURFACE_SRP_BATCHER_OK', 'Exiting batchmode successfully'])
    base.require_log('Basement-arena-horror-play.log', ['BASEMENT_SURFACE_PLAYMODE_OK', 'BASEMENT_ARENA_FLICKER_RUNTIME_OK'])
    manifest = json.loads((BEFORE / 'manifest.json').read_text())
    for name, expected in manifest.items():
        if name in [RUNTIME, CONVERTER, SCENE]:
            assert (ROOT / name).read_bytes() == (PREVIEW / name).read_bytes()
        elif name in TARGETS:
            before = base.blocks((BEFORE / name).read_text(encoding='utf-8-sig'))
            installed = base.blocks((ROOT / name).read_text(encoding='utf-8-sig'))
            assert before.keys() <= installed.keys(), 'An original prefab object was removed: ' + name
            for fid, (kind, block) in before.items():
                if kind in [33, 65, 64, 54, 136, 1001] or (kind == 114 and base.script(block) not in base.VISUAL_SCRIPTS):
                    assert installed[fid][1] == block, 'Previously installed prefab changed gameplay/collision: ' + name + ' ' + fid
        else:
            assert hashlib.sha256((ROOT / name).read_bytes()).hexdigest() == expected, 'Concurrent modification: ' + name
    for path in TARGETS:
        shutil.copy2(PREVIEW / path, ROOT / path)
    dependencies = set((PREVIEW / 'Preview/ArenaHorror/dependencies.txt').read_text().splitlines())
    for path in (PREVIEW / ART).rglob('*'):
        if path.is_file():
            asset = str(path.relative_to(PREVIEW)).replace('\\', '/').removesuffix('.meta')
            if '/Textures/' not in asset and not asset.endswith('.json') and asset not in dependencies:
                continue
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
    source_root = PREVIEW / 'Preview/ArenaHorror/HumanSource'
    source_dest = ROOT / 'Tools/Basement/Horror/Source/MakeHuman'
    source_dest.mkdir(parents=True, exist_ok=True)
    records = json.loads((source_root / 'model-sources.json').read_text())
    for record in records:
        name = record['file']
        source = PREVIEW / ART / name if name.startswith('Textures/') else source_root / name
        assert hashlib.sha256(source.read_bytes()).hexdigest() == record['sha256']
        if not name.startswith('Textures/'):
            shutil.copy2(source, source_dest / name)
            record['file'] = 'Tools/Basement/Horror/Source/MakeHuman/' + name
        else:
            record['file'] = ART + '/' + name
    shutil.copy2(source_root / 'LICENSE-MakeHuman.md', source_dest / 'LICENSE-MakeHuman.md')
    (source_dest / 'model-sources.json').write_text(json.dumps(records, indent=2))
    shutil.copy2(PREVIEW / 'Preview/ArenaHorror/Sources/pose-metrics.json', ROOT / 'Tools/Basement/Horror/pose-metrics.json')
    shutil.copy2(PREVIEW / 'Preview/ArenaHorror/Sources/dismemberment-metrics.json', ROOT / 'Tools/Basement/Horror/dismemberment-metrics.json')
    print('Arena refinement installed. Existing solid collision, boss room, main scene and encounter unchanged.')


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--install', action='store_true')
    args = parser.parse_args()
    install() if args.install else stage()
