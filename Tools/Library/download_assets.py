"""Download CC0 source models and PNG maps from official Poly Haven manifests."""
import concurrent.futures
import hashlib
import json
import time
import urllib.request
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
WORK = ROOT / '.utmp/LibraryPreview'
ART = WORK / 'Assets/_Project/Scenes/Library'
MODELS = ['wooden_bookshelf_worn', 'book_encyclopedia_set_01', 'dining_chair_02',
          'wooden_table_02', 'marble_bust_01', 'wooden_ladder', 'drawer_cabinet']
SURFACES = ['dirty_carpet', 'worn_mossy_plasterwall']

def fetch(url):
    for attempt in range(4):
        try:
            req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
            with urllib.request.urlopen(req, timeout=60) as response:
                return response.read()
        except Exception:
            if attempt == 3:
                raise
            time.sleep(1 + attempt)

def download(asset):
    manifest = json.loads(fetch('https://api.polyhaven.com/files/' + asset))
    info = json.loads(fetch('https://api.polyhaven.com/info/' + asset))
    records = []
    if asset == 'book_encyclopedia_set_01':
        roles = ['cover_diff', 'cover_nor_gl', 'cover_arm', 'paper_diff', 'paper_nor_gl', 'paper_arm']
    else:
        roles = ['Diffuse', 'nor_gl', 'arm']
    entries = [(role, manifest[role]['1k']['png']) for role in roles]
    if asset in MODELS:
        entries.insert(0, ('model', manifest['fbx']['1k']['fbx']))
    for role, item in entries:
        path = ART / (f'Models/{asset}/model.fbx' if role == 'model' else f'Textures/{asset}/{role}.png')
        path.parent.mkdir(parents=True, exist_ok=True)
        data = path.read_bytes() if path.exists() else fetch(item['url'])
        if item.get('md5') and hashlib.md5(data).hexdigest() != item['md5']:
            raise ValueError('Hash mismatch: ' + asset + '/' + role)
        if role != 'model' and not data.startswith(b'\x89PNG\r\n\x1a\n'):
            raise ValueError('Invalid PNG: ' + item['url'])
        path.write_bytes(data)
        records.append(dict(asset=asset, role=role, file=path.relative_to(WORK).as_posix(),
                            url=item['url'], page='https://polyhaven.com/a/' + asset,
                            license='CC0-1.0', license_url='https://polyhaven.com/license',
                            authors=info.get('authors'), sha256=hashlib.sha256(data).hexdigest(), bytes=len(data)))
    (ROOT / 'Tools/Library/Manifests').mkdir(exist_ok=True)
    (ROOT / 'Tools/Library/Manifests' / (asset + '.json')).write_text(json.dumps(manifest, indent=2))
    print(asset + ': source files verified', flush=True)
    return records

if __name__ == '__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        records = [record for result in pool.map(download, MODELS + SURFACES) for record in result]
    (ROOT / 'Tools/Library/asset-sources.json').write_text(json.dumps(records, indent=2))
