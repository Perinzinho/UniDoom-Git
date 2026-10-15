"""Snapshot, stage minimal mesh edits, and install the validated inscriptions."""
from pathlib import Path
import hashlib
import importlib.util
import json
import re
import shutil
import sys

sys.dont_write_bytecode = True

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/BasementSigilInscription'
PREVIEW = ROOT / '.utmp/BasementPreview'
ART = 'Assets/Art/Basement/SigilInscription'
SPEC = importlib.util.spec_from_file_location('visual_install', Path(__file__).with_name('install.py'))
HELPER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HELPER)
PREFABS = HELPER.PREFABS


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def prepare():
    assert not (WORK / 'baseline.json').exists(), 'Do not overwrite the snapshot'
    paths = set(PREFABS + ['Assets/Scenes/SampleScene.unity'])
    for folder in ['Assets/Art/Basement/VisualRepair', 'Assets/TotalChaos']:
        paths.update(p.relative_to(ROOT).as_posix() for p in (ROOT / folder).rglob('*') if p.is_file())
    baseline = {}
    for path in sorted(paths):
        source = ROOT / path
        baseline[path] = sha(source)
        snapshot = WORK / 'before' / path
        snapshot.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, snapshot)
        destination = PREVIEW / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        if not destination.exists() or sha(destination) != baseline[path]:
            shutil.copy2(source, destination)
    (WORK / 'baseline.json').write_text(json.dumps(baseline, indent=2), encoding='utf-8')
    for name in ['BasementSigilInscription.cs', 'BasementVisualRepair.cs']:
        shutil.copy2(Path(__file__).with_name(name), PREVIEW / 'Assets/Editor' / name)
    print('Snapshot saved; isolated project synchronized.')


def stage():
    log = (WORK / 'build.log').read_text(encoding='utf-8', errors='replace')
    assert 'BASEMENT_SIGILS_BUILT' in log and 'Exiting batchmode successfully' in log
    assert not any(e in log for e in ['Exception:', 'error CS', 'Shader error in'])
    report = []
    for path in PREFABS:
        original = (WORK / 'before' / path).read_text(encoding='utf-8-sig')
        generated = (PREVIEW / path).read_text(encoding='utf-8-sig')
        old, new = HELPER.blocks(original), HELPER.blocks(generated)
        assert old.keys() <= new.keys(), 'Original object removed'
        output = original[:original.index('--- !u!')]
        changes = []
        for fid, (kind, text) in old.items():
            assert new[fid][0] == kind
            a, b = HELPER.fields(text), HELPER.fields(new[fid][1])
            for field in {4: {'m_Children'}, 33: {'m_Mesh'}}.get(kind, set()):
                if a.get(field) == b.get(field):
                    continue
                assert field in a and field in b
                if field == 'm_Children':
                    before = re.findall(r'fileID: (-?\d+)', a[field])
                    after = re.findall(r'fileID: (-?\d+)', b[field])
                    assert after[:len(before)] == before
                    assert all(child not in old for child in after[len(before):])
                else:
                    assert kind == 33
                text = text.replace(a[field], b[field], 1)
                changes.append([fid, kind, field])
            output += text
        added = new.keys() - old.keys()
        assert all(new[fid][0] in {1, 4, 23, 33} for fid in added)
        assert len(added) == (10 if path.endswith('BasementFinalSanctum.prefab') else 0)
        output += ''.join(text for fid, (_, text) in new.items() if fid in added)
        destination = WORK / 'staged' / path
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_text(output, encoding='utf-8', newline='\n')
        (PREVIEW / path).write_bytes(destination.read_bytes())
        report.append({'path': path, 'new_visual_blocks': len(added), 'mesh_fields_and_child_lists': changes})
    (WORK / 'staging-report.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('Only mesh references and appended visual children staged. All other original fields preserved.')


def install():
    log = (WORK / 'verify.log').read_text(encoding='utf-8', errors='replace')
    for marker in ['BASEMENT_SIGILS_VERIFIED', 'BASEMENT_VISUAL_REPAIR_VERIFIED', 'BASEMENT_FINAL_MAP_PLAYER_OK', 'BASEMENT_FINAL_PREFAB_PLAYER_OK', 'BASEMENT_ENCOUNTER_TEST_OK', 'Exiting batchmode successfully']:
        assert marker in log, marker
    assert not any(e in log for e in ['Exception:', 'error CS', 'Shader error in', 'Render Graph Execution error'])
    baseline = json.loads((WORK / 'baseline.json').read_text())
    for path, digest in baseline.items():
        assert sha(ROOT / path) == digest, 'Concurrent root edit: ' + path
    for path in PREFABS:
        assert sha(PREVIEW / path) == sha(WORK / 'staged' / path), 'Validated prefab changed'
    assert not (ROOT / ART).exists(), 'Do not overwrite existing art'
    shutil.copytree(PREVIEW / ART, ROOT / ART)
    shutil.copy2(PREVIEW / (ART + '.meta'), ROOT / (ART + '.meta'))
    for path in PREFABS:
        (ROOT / path).write_bytes((WORK / 'staged' / path).read_bytes())
        assert sha(ROOT / path) == sha(PREVIEW / path)
    for source in (PREVIEW / ART).rglob('*'):
        if source.is_file():
            assert sha(source) == sha(ROOT / source.relative_to(PREVIEW))
    docs = Path(__file__).parent / 'Sigils'
    docs.mkdir(exist_ok=True)
    for source in (PREVIEW / 'Preview/BasementSurface').glob('Sigils-*.png'):
        shutil.copy2(source, docs / source.name.removeprefix('Sigils-'))
    for source in ['phrases.txt', 'staging-report.json']:
        shutil.copy2(WORK / source, docs / source)
    shutil.copy2(PREVIEW / 'Preview/VisualRepair/validation.txt', docs / 'validation.txt')
    verification = {
        'unity': '6000.5.7f1',
        'prefab_mesh_checks': re.findall(r'BASEMENT_SIGILS_PREFAB_OK ([^\r\n]+)', log),
        'glyphs': 'One vector glyph per normalized letter; spaces separate words.',
        'gameplay': 'CharacterController traversal in map and prefab, gate and encounter passed.',
        'ordinary_lettering': 'Absent, including nested prefabs.',
        'compile_render_errors': [],
        'scene_sha256_unchanged': sha(ROOT / 'Assets/Scenes/SampleScene.unity'),
        'player_build_or_fps_measurement': False
    }
    (docs / 'verification.json').write_text(json.dumps(verification, indent=2), encoding='utf-8')
    # A code-native vector legend for reviewing the transcription, outside Assets.
    alphabet = re.findall(r"\{'([A-Z])', \"([^\"]+)\"\}", Path(__file__).with_name('BasementSigilInscription.cs').read_text(encoding='utf-8'))
    assert len(alphabet) == 26
    svg = ['<svg xmlns="http://www.w3.org/2000/svg" width="900" height="390" viewBox="0 0 900 390">', '<rect width="900" height="390" fill="#171717"/>']
    for i, (letter, strokes) in enumerate(alphabet):
        x, y = 15 + i % 10 * 88, 12 + i // 10 * 126
        svg.append(f'<g transform="translate({x} {y}) scale(.7)" fill="none" stroke="#e3d9c7" stroke-width="3.8" stroke-linecap="round" stroke-linejoin="round">')
        svg.extend(f'<polyline points="{stroke}"/>' for stroke in strokes.split(';'))
        svg.append('</g>')
        svg.append(f'<text x="{x + 35}" y="{y + 98}" text-anchor="middle" fill="#e3d9c7" font-family="sans-serif" font-size="19">{letter}</text>')
    svg.append('</svg>')
    (docs / 'alphabet.svg').write_text('\n'.join(svg), encoding='utf-8')
    print('Installed two seals with three encoded phrases each. Scene, gameplay and earlier shader optimization preserved.')


if __name__ == '__main__':
    {'prepare': prepare, 'stage': stage, 'install': install}[sys.argv[1]]()
