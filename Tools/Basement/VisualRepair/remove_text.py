"""Install validated text removal without changing other prefab fields."""
import hashlib
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/BasementTextRemoval'
PREVIEW = ROOT / '.utmp/BasementPreview'


def blocks(text):
    return {m[2]: (int(m[1]), m[0]) for m in re.finditer(
        r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)', text, re.S)}


def fields(text):
    return {m[1]: m[0] for m in re.finditer(r'^  (\w+):[^\n]*(?:\n(?!  \w+:)[^\n]*)*', text, re.M)}


def main():
    log = (WORK / 'remove.log').read_text(encoding='utf-8', errors='replace')
    for marker in ['BASEMENT_ALL_TEXT_REMOVED', 'BASEMENT_VISUAL_REPAIR_VERIFIED', 'Exiting batchmode successfully']:
        assert marker in log, marker
    assert not any(error in log for error in ['Exception:', 'error CS', 'Shader error in'])
    baseline = json.loads((WORK / 'baseline.json').read_text())
    pending, report = {}, []
    scene = (ROOT / 'Assets/Scenes/SampleScene.unity').read_text(encoding='utf-8-sig')
    for path, digest in baseline.items():
        assert hashlib.sha256((ROOT / path).read_bytes()).hexdigest() == digest, 'Concurrent edit: ' + path
        original = (WORK / 'before' / path).read_text(encoding='utf-8-sig')
        generated = (PREVIEW / path).read_text(encoding='utf-8-sig')
        old, new = blocks(original), blocks(generated)
        assert new.keys() <= old.keys(), 'Unexpected new prefab object'
        sign_objects = {fid for fid, (kind, text) in old.items() if kind == 1 and '  m_Name: Physical sign face\n' in text}
        sign_ids = set(sign_objects)
        for fid in sign_objects:
            sign_ids.update(re.findall(r'component: {fileID: (-?\d+)}', old[fid][1]))
        removed = old.keys() - new.keys()
        assert removed == sign_ids, 'Removed object outside the lettering signs'
        for fid in removed:
            assert not re.search(r'fileID: ' + fid + r'[,}]', scene), 'Scene references removed lettering object'
        output = original[:original.index('--- !u!')]
        changes = []
        for fid, (kind, text) in old.items():
            if fid in removed:
                continue
            assert new[fid][0] == kind
            a, b = fields(text), fields(new[fid][1])
            for field in {4: {'m_Children'}, 23: {'m_Enabled'}, 102: {'m_Text'}}.get(kind, set()):
                if a.get(field) == b.get(field):
                    continue
                assert field in a and field in b
                if field == 'm_Children':
                    expected = [i for i in re.findall(r'fileID: (-?\d+)', a[field]) if i not in removed]
                    assert re.findall(r'fileID: (-?\d+)', b[field]) == expected
                elif field == 'm_Text':
                    assert not re.sub(r'^  m_Text:\s*', '', b[field]).strip().strip('"')
                else:
                    assert b[field].strip() == 'm_Enabled: 0'
                text = text.replace(a[field], b[field], 1)
                changes.append([fid, kind, field])
            output += text
        # Reordering and ordinary Unity serialization noise are ignored; fields
        # outside this explicit removal remain byte-identical to the snapshot.
        pending[path] = output.encode('utf-8')
        report.append({'path': path, 'removed_sign_objects': len(sign_objects), 'removed_sign_blocks': len(removed), 'changes': changes})
    for path, data in pending.items():
        (ROOT / path).write_bytes(data)
    (ROOT / 'Tools/Basement/VisualRepair/text-removal.json').write_text(json.dumps(report, indent=2), encoding='utf-8')
    print('Removed six textured signs; cleared remaining arena lettering. Physics, meshes and scripts preserved.')


if __name__ == '__main__':
    main()
