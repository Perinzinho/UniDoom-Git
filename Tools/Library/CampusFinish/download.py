import concurrent.futures
import hashlib
import json
import sys
from pathlib import Path
ROOT=Path(__file__).resolve().parents[3]
sys.path.insert(0,str(ROOT/'Tools/Library'))
from download_assets import fetch
OUT=ROOT/'Tools/Library/CampusFinish'
WORK=ROOT/'.utmp/LibraryPreview'

def download(asset):
    manifest=json.loads(fetch('https://api.polyhaven.com/files/'+asset))
    info=json.loads(fetch('https://api.polyhaven.com/info/'+asset))
    (OUT/(asset+'.json')).write_text(json.dumps(manifest,indent=2))
    result=[]
    for role in ['Diffuse','nor_gl','arm']:
        entry=manifest[role]['2k']['png'];data=fetch(entry['url'])
        assert hashlib.md5(data).hexdigest()==entry['md5']
        assert data.startswith(b'\x89PNG\r\n\x1a\n')
        path=WORK/'Assets/_Project/Scenes/Library/Textures/CampusFinish'/asset/(role+'.png')
        path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
        result.append(dict(asset=asset,role=role,path=path.relative_to(WORK).as_posix(),url=entry['url'],page='https://polyhaven.com/a/'+asset,license='CC0-1.0',authors=info.get('authors'),sha256=hashlib.sha256(data).hexdigest()))
    print(asset+': downloaded and verified',flush=True)
    return result

if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=2) as pool:records=[r for result in pool.map(download,['painted_plaster_wall','white_plaster_02']) for r in result]
    (OUT/'sources.json').write_text(json.dumps(records,indent=2))
