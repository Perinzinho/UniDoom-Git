import hashlib
import json
import sys
from pathlib import Path

OUT=Path(__file__).resolve().parent
ROOT=OUT.parents[2]
sys.path.insert(0,str(ROOT/'Tools/Library'))
from download_assets import fetch
WORK=ROOT/'.utmp/EntranceRebuild'
asset='grass_ground'
manifest=json.loads(fetch('https://api.polyhaven.com/files/'+asset))
info=json.loads(fetch('https://api.polyhaven.com/info/'+asset))
(OUT/'grass_ground.json').write_text(json.dumps(manifest,indent=2))
records=[]
for role in ['Diffuse','nor_gl','arm']:
    entry=manifest[role]['2k']['png'];data=fetch(entry['url'])
    assert hashlib.md5(data).hexdigest()==entry['md5']
    path=WORK/f'Assets/_Project/Global/Textures/CampusNight/{asset}/{role}.png'
    path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
    records.append(dict(path=path.relative_to(WORK).as_posix(),url=entry['url'],page='https://polyhaven.com/a/'+asset,license='CC0-1.0',authors=info.get('authors'),sha256=hashlib.sha256(data).hexdigest()))
(OUT/'sources.json').write_text(json.dumps(records,indent=2))
print('Verified grass ground PNG 2K maps: albedo, normal and ARM, CC0.')
