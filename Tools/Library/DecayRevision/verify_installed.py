"""Check the installed revision against its pre-edit snapshot and Unity evidence."""
import concurrent.futures
import hashlib
import json
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Tools/Library/DecayRevision'
PREFAB='Assets/_Project/Scenes/Library/Prefabs/Library.prefab'

def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
def check(item):
    name,before=item
    path=ROOT/name
    return name if not path.exists() or sha(path)!=before else None

def main():
    before=json.loads((OUT/'before-manifest.json').read_text())
    installed=json.loads((OUT/'installed.json').read_text())
    with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:
        changed=sorted(name for name in pool.map(check,before.items()) if name)
    concurrent_scene=json.loads((OUT/'concurrent-scene.json').read_text()) if (OUT/'concurrent-scene.json').exists() else None
    allowed=[PREFAB]
    if concurrent_scene:
        assert sha(ROOT/concurrent_scene['path'])==concurrent_scene['preserved_sha256'],'Scene changed again during validation'
        allowed.append(concurrent_scene['path'])
        log=(OUT/'unity-latest-scene.log').read_text(errors='replace')
        assert 'LIBRARY_DECAY_VERIFY_PASS' in log and 'Exiting batchmode successfully' in log
    assert changed==sorted(allowed),changed
    for item in installed:
        path=ROOT/item['path'];assert sha(path)==item['sha256'],item['path']
        if path.suffix!='.meta':assert Path(str(path)+'.meta').is_file(),'Missing meta: '+item['path']
        for folder in path.parents:
            if folder==ROOT/'Assets':break
            assert Path(str(folder)+'.meta').is_file(),'Missing folder meta: '+str(folder)
    # GUIDs are checked across every asset meta, including old, unreferenced content.
    guids={}
    for path in (ROOT/'Assets').rglob('*.meta'):
        match=re.search(r'^guid: ([0-9a-f]{32})$',path.read_text(errors='replace'),re.M)
        if match:guids.setdefault(match[1],[]).append(path.relative_to(ROOT).as_posix())
    new_paths={item['path'] for item in installed if item['path'] not in before}
    duplicates={guid:paths for guid,paths in guids.items() if len(paths)>1 and set(paths)&new_paths}
    assert not duplicates,duplicates
    assert (OUT/'validation.txt').read_text().startswith('PASS\n')
    assert (OUT/'play-validation.txt').read_text().startswith('PASS\n')
    result=dict(status='PASS',baseline_files=len(before),changed_existing_files=changed,
                installed_files=len(installed),new_guid_collisions=duplicates,
                main_scene_sha256=sha(ROOT/'Assets/_Project/Scenes/Main/SampleScene.unity'),
                prefab_sha256=sha(ROOT/PREFAB),main_scene_baseline_unchanged=not concurrent_scene,
                concurrent_scene_change_preserved=concurrent_scene,
                limits='No standalone build, full combat test, lightmap bake or target-hardware FPS benchmark.')
    (OUT/'installed-validation.json').write_text(json.dumps(result,indent=2))
    print(json.dumps(result,indent=2))

if __name__=='__main__':main()
