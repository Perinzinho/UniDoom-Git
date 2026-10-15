"""Download only selected production files from the public Poly Haven API."""
from pathlib import Path
import urllib.request, json, hashlib, concurrent.futures

ROOT=Path(__file__).resolve().parents[3]
DEST=ROOT/'.utmp/BasementPreview/Assets/Art/Basement/Ritual'
HEADERS={'User-Agent':'UniDoom ritual-room asset integration (personal Unity project)'}
def get(url):
    return urllib.request.urlopen(urllib.request.Request(url,headers=HEADERS),timeout=120).read()
def fetch(task):
    rel,info=task
    p=DEST/rel
    p.parent.mkdir(parents=True,exist_ok=True)
    data=p.read_bytes() if p.exists() else get(info['url'])
    assert hashlib.md5(data).hexdigest()==info['md5'],rel
    p.write_bytes(data)
    return dict(path=rel,url=info['url'],md5=info['md5'],sha256=hashlib.sha256(data).hexdigest(),bytes=len(data))

tasks=[];assets=[]
for asset in ['brass_candleholders','brass_goblets','monastery_stone_floor','old_stone_wall']:
    files=json.loads(get('https://api.polyhaven.com/files/'+asset))
    models=asset.startswith('brass_')
    assets.append(dict(id=asset,source='https://polyhaven.com/a/'+asset,license='CC0-1.0',license_url='https://polyhaven.com/license'))
    if models:
        tasks.append(('Models/'+asset+'/'+asset+'.fbx',files['fbx']['1k']['fbx']))
        for key,v in files.items():
            if key.endswith(('_diff','_nor_gl','_arm')):
                tasks.append(('Models/'+asset+'/'+key+'.png',v['1k']['png']))
    else:
        for key,name in [('Diffuse','diff'),('nor_gl','nor_gl'),('arm','arm')]:
            tasks.append(('Textures/'+asset+'_'+name+'.png',files[key]['2k']['png']))
with concurrent.futures.ThreadPoolExecutor(max_workers=6) as pool:
    records=list(pool.map(fetch,tasks))
DEST.mkdir(parents=True,exist_ok=True)
(DEST/'asset-sources.json').write_text(json.dumps(dict(assets=assets,files=records),indent=2))
print('Downloaded and hash-verified',len(records),'files;',sum(r['bytes'] for r in records)//1024//1024,'MiB')
