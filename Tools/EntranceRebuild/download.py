import concurrent.futures
import hashlib
import json
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
sys.path.insert(0,str(ROOT/'Tools/Library'))
from download_assets import fetch
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/EntranceRebuild'
SHARED='Assets/_Project/Global'
MODELS={
 'painted_wooden_bench':'EntranceGarden', 'fern_02':'EntranceGarden',
 'dead_tree_trunk_02':'EntranceGarden','metal_office_desk':'DirectorRoom',
 'mid_century_lounge_chair':'DirectorRoom','desk_lamp_arm_01':'DirectorRoom',
 'vintage_telephone_wall_clock':'EntranceHallway','industrial_wall_lamp':None,
 'potted_plant_02':'DirectorRoom','korean_fire_extinguisher_01':'EntranceHallway','tree_small_02':'EntranceGarden'}
SURFACES={'cobblestone_floor_08':'EntranceGarden','herringbone_parquet':'DirectorRoom','wood_table':'DirectorRoom','mossy_brick':'EntranceGarden'}

def download(asset):
    manifest=json.loads((OUT/'Manifests'/(asset+'.json')).read_text())
    info=json.loads(fetch('https://api.polyhaven.com/info/'+asset))
    room=MODELS.get(asset,SURFACES.get(asset))
    base=f'Assets/_Project/Scenes/{room}' if room else SHARED
    roles=['Diffuse','nor_gl','arm']
    if asset=='potted_plant_02':roles=['pot_diff','pot_nor_gl','pot_arm','leaves_diff','leaves_nor_gl','leaves_arm','leaves_alpha']
    if asset=='fern_02':roles+=['Alpha']
    if asset=='industrial_wall_lamp':roles+=['emissive','glass_diff','glass_nor_gl','glass_arm','glass_alpha']
    if asset=='korean_fire_extinguisher_01':roles=['body_diff','body_nor_gl','body_arm','paper_diff','paper_nor_gl','paper_arm','glass_diff','glass_nor_gl','glass_arm']
    if asset=='tree_small_02':roles=['Diffuse','nor_gl','arm','branch_diff','branch_nor_gl','branch_arm']
    entries=[(role,manifest[role]['2k']['png']) for role in roles]
    if asset in MODELS:entries.insert(0,('model',manifest['fbx']['1k']['fbx']))
    records=[]
    for role,entry in entries:
        rel=f'{base}/'+(f'Models/EntranceRebuild/{asset}/model.fbx' if role=='model' else f'Textures/EntranceRebuild/{asset}/{role}.png')
        path=WORK/rel
        path.parent.mkdir(parents=True,exist_ok=True)
        data=path.read_bytes() if path.exists() else fetch(entry['url'])
        if hashlib.md5(data).hexdigest()!=entry['md5']:data=fetch(entry['url'])
        assert hashlib.md5(data).hexdigest()==entry['md5'],rel
        if role!='model':assert data.startswith(b'\x89PNG\r\n\x1a\n')
        path.write_bytes(data)
        records.append(dict(asset=asset,role=role,file=rel,url=entry['url'],page='https://polyhaven.com/a/'+asset,license='CC0-1.0',authors=info.get('authors'),sha256=hashlib.sha256(data).hexdigest(),bytes=len(data)))
    print(asset+': verified',flush=True)
    return records

if __name__=='__main__':
    with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
        records=[r for group in pool.map(download,list(MODELS)+list(SURFACES)) for r in group]
    (OUT/'sources.json').write_text(json.dumps(records,indent=2))
