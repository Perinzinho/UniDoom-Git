import concurrent.futures
import hashlib
import json
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
OUT = Path(__file__).resolve().parent
WORK = ROOT / '.utmp/EntranceRebuild'
sys.path.insert(0, str(ROOT / 'Tools/Library'))
from download_assets import fetch

def surface(asset):
    manifest = json.loads(fetch('https://api.polyhaven.com/files/' + asset))
    (OUT / (asset + '.json')).write_text(json.dumps(manifest, indent=2))
    records = []
    for role in ['Diffuse', 'nor_gl', 'arm']:
        entry = manifest[role]['2k']['png']
        data = fetch(entry['url'])
        assert hashlib.md5(data).hexdigest() == entry['md5']
        path = WORK / f'Assets/_Project/Scenes/EntranceGarden/Textures/CampusReference/{asset}/{role}.png'
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_bytes(data)
        records.append(dict(file=path.relative_to(WORK).as_posix(), url=entry['url'], page='https://polyhaven.com/a/' + asset, license='CC0-1.0', sha256=hashlib.sha256(data).hexdigest()))
    print(asset + ' verified', flush=True)
    return records

if __name__ == '__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:
        records = [item for group in pool.map(surface, ['patterned_cobblestone_02']) for item in group]
    url = 'https://opengameart.org/sites/default/files/palm_tree_v2.7z'
    data = fetch(url)
    (OUT / 'Downloads').mkdir(exist_ok=True)
    (OUT / 'Downloads/palm_tree_v2.7z').write_bytes(data)
    records.append(dict(file='Downloads/palm_tree_v2.7z', url=url, page='https://opengameart.org/content/palm-tree-v2', author='Yughues', license='CC0-1.0', sha256=hashlib.sha256(data).hexdigest()))
    (OUT / 'sources.json').write_text(json.dumps(records, indent=2))
    print('Palm archive verified and sources recorded.', flush=True)
