"""Fetch the additional CC0 model and source PNG maps for the abandoned revision."""
import concurrent.futures
import hashlib
import json
import sys
from pathlib import Path
sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from download_assets import fetch

ROOT=Path(__file__).resolve().parents[3]
OUT=ROOT/'Tools/Library/DecayRevision'
WORK=ROOT/'.utmp/LibraryPreview'
ART=WORK/'Assets/_Project/Scenes/Library'

def download(asset):
    manifest=json.loads(fetch('https://api.polyhaven.com/files/'+asset))
    info=json.loads(fetch('https://api.polyhaven.com/info/'+asset))
    roles=['Diffuse','nor_gl','arm'];entries=[(role,manifest[role]['1k' if asset=='ceiling_fan' else '2k']['png']) for role in roles]
    if asset=='ceiling_fan':entries.insert(0,('model',manifest['fbx']['1k']['fbx']))
    records=[]
    for role,item in entries:
        target=ART/(f'Models/DecayRevision/{asset}/model.fbx' if role=='model' else f'Textures/DecayRevision/{asset}/{role}.png')
        target.parent.mkdir(parents=True,exist_ok=True)
        data=fetch(item['url'])
        assert hashlib.md5(data).hexdigest()==item['md5']
        if role!='model':assert data.startswith(b'\x89PNG\r\n\x1a\n')
        target.write_bytes(data)
        records.append(dict(asset=asset,role=role,file=target.relative_to(WORK).as_posix(),url=item['url'],page='https://polyhaven.com/a/'+asset,license='CC0-1.0',license_url='https://polyhaven.com/license',sha256=hashlib.sha256(data).hexdigest(),authors=info.get('authors')))
    print(asset+': downloaded and verified',flush=True)
    return records

if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        records=[r for records in pool.map(download,['ceiling_fan','damaged_plaster','concrete_floor_damaged_01']) for r in records]
    (OUT/'asset-sources.json').write_text(json.dumps(records,indent=2))
