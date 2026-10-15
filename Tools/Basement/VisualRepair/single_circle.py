"""Revise the shared seal meshes; keep prefab bytes, GUIDs and scene unchanged."""
from pathlib import Path
import hashlib
import importlib.util
import json
import re
import shutil
import sys

sys.dont_write_bytecode = True
ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/BasementSingleSigilCircle'
PREVIEW = ROOT / '.utmp/BasementPreview'
ART = 'Assets/Art/Basement/SigilInscription'
SPEC = importlib.util.spec_from_file_location('sigils', Path(__file__).with_name('sigils.py'))
SHARED = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(SHARED)
MESHES = {f'{ART}/Meshes/{place}_{part}.asset' for place in ['Preparation', 'Sanctum'] for part in ['Contours', 'Phrases']}


def prepare():
    assert not (WORK / 'baseline.json').exists(), 'Never overwrite the snapshot'
    paths = set(SHARED.PREFABS + ['Assets/Scenes/SampleScene.unity', ART + '.meta'])
    paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT / ART).rglob('*') if p.is_file())
    baseline = {}
    for path in paths:
        source = ROOT / path
        baseline[path] = SHARED.sha(source)
        snapshot = WORK / 'before' / path
        snapshot.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, snapshot)
        shutil.copy2(source, PREVIEW / path)
    for name in ['BasementSigilInscription.cs', 'BasementVisualRepair.cs', 'Sigils/README.md', 'README.md']:
        source = Path(__file__).parent / name
        snapshot = WORK / 'before/Tools/Basement/VisualRepair' / name
        snapshot.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, snapshot)
    (WORK / 'baseline.json').write_text(json.dumps(baseline, indent=2), encoding='utf-8')
    print('Snapshotted current meshes, prefabs, scene and authoring files.')


def stage():
    log = (WORK / 'build.log').read_text(encoding='utf-8', errors='replace')
    assert 'BASEMENT_SIGILS_BUILT' in log and 'Exiting batchmode successfully' in log
    assert not any(e in log for e in ['Exception:', 'error CS', 'Shader error in'])
    for path in SHARED.PREFABS:
        original = (WORK / 'before' / path).read_text(encoding='utf-8-sig')
        generated = (PREVIEW / path).read_text(encoding='utf-8-sig')
        old, new = SHARED.HELPER.blocks(original), SHARED.HELPER.blocks(generated)
        assert old.keys() == new.keys(), 'Hierarchy changed'
        for fid, (kind, block) in old.items():
            assert new[fid][0] == kind
            if kind == 33:
                assert SHARED.HELPER.fields(block)['m_Mesh'] == SHARED.HELPER.fields(new[fid][1])['m_Mesh'], 'Mesh GUID changed'
        # Preserve original prefab bytes; only the referenced meshes are revised.
        (PREVIEW / path).write_bytes((WORK / 'before' / path).read_bytes())
    print('Prefab IDs and mesh GUIDs unchanged; restored original prefab bytes for validation.')


def install():
    log = (WORK / 'verify.log').read_text(encoding='utf-8', errors='replace')
    for marker in ['BASEMENT_SIGILS_VERIFIED', 'BASEMENT_VISUAL_REPAIR_VERIFIED', 'BASEMENT_FINAL_MAP_PLAYER_OK', 'BASEMENT_FINAL_PREFAB_PLAYER_OK', 'BASEMENT_ENCOUNTER_TEST_OK', 'Exiting batchmode successfully']:
        assert marker in log, marker
    assert not any(e in log for e in ['Exception:', 'error CS', 'Shader error in', 'Render Graph Execution error'])
    baseline = json.loads((WORK / 'baseline.json').read_text())
    changed = set()
    for path, digest in baseline.items():
        assert SHARED.sha(ROOT / path) == digest, 'Concurrent root edit: ' + path
        if SHARED.sha(PREVIEW / path) != digest:
            changed.add(path)
    assert changed == MESHES, 'Unexpected preview changes: ' + str(changed ^ MESHES)
    for path in sorted(changed):
        shutil.copy2(PREVIEW / path, ROOT / path)
        assert SHARED.sha(ROOT / path) == SHARED.sha(PREVIEW / path)
    docs = Path(__file__).parent / 'Sigils'
    for source in (PREVIEW / 'Preview/BasementSurface').glob('Sigils-*.png'):
        shutil.copy2(source, docs / source.name.removeprefix('Sigils-'))
    shutil.copy2(PREVIEW / 'Preview/VisualRepair/validation.txt', docs / 'validation.txt')
    shutil.copy2(WORK / 'phrases.txt', docs / 'phrases.txt')
    (docs / 'single-circle-validation.json').write_text(json.dumps({
        'changed_assets': sorted(changed), 'prefabs_scene_and_meta_unchanged': True,
        'shape': 'pentagram with one circular row containing the three phrases',
        'unity': '6000.5.7f1', 'traversal_gate_encounter': 'passed',
        'compile_render_errors': [], 'player_build_or_fps_measurement': False
    }, indent=2), encoding='utf-8')
    (docs / 'verification.json').write_text(json.dumps({
        'unity': '6000.5.7f1',
        'prefab_mesh_checks': re.findall(r'BASEMENT_SIGILS_PREFAB_OK ([^\r\n]+)', log),
        'shape': 'pentagram; one circular row with all three phrases',
        'gameplay': 'CharacterController traversal in map and prefab, gate and encounter passed.',
        'ordinary_lettering': 'Absent, including nested prefabs.',
        'compile_render_errors': [], 'player_build_or_fps_measurement': False
    }, indent=2), encoding='utf-8')
    print('Installed four shared meshes. Prefabs, scene, GUIDs, materials and gameplay unchanged.')


if __name__ == '__main__':
    {'prepare': prepare, 'stage': stage, 'install': install}[sys.argv[1]]()
