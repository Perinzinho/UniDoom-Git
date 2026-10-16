import hashlib
import json
import re
import shutil
import sys
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/EntranceRebuild'
PREFAB='Assets/_Project/Scenes/DirectorRoom/Prefabs/DirectorRoom.prefab'
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(text):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',text,re.S)}
def key(block):return tuple(re.search(rf'^  {field}:.*$',block,re.M)[0] for field in ['m_CorrespondingSourceObject','m_PrefabInstance'])

if __name__=='__main__':
    before=json.loads((OUT/'before.json').read_text())
    old=blocks((OUT/'Before'/PREFAB).read_text(encoding='utf-8'))
    path=WORK/PREFAB;text=path.read_text(encoding='utf-8');new=blocks(text)
    if '--normalize' in sys.argv:
        for fid in old.keys()-new.keys():
            kind,block=old[fid];assert kind==4 and 'stripped' in block.splitlines()[0]
            match=[i for i,(k,b) in new.items() if k==4 and 'stripped' in b.splitlines()[0] and key(b)==key(block)]
            assert len(match)==1
            text=text.replace(f'--- !u!4 &{match[0]} stripped',f'--- !u!4 &{fid} stripped')
            text=re.sub(rf'fileID: {match[0]}(?=[,}}])',f'fileID: {fid}',text)
        path.write_text(text,encoding='utf-8',newline='\n')
        assert old.keys()==blocks(text).keys();print('All existing prefab fileIDs preserved.');sys.exit(0)
    assert old.keys()==new.keys(),'Existing object/component IDs changed'
    log=(WORK/'furniture-verify.log').read_text(errors='replace')
    assert 'DIRECTOR_FURNITURE_VERIFY_PASS' in log
    assert 'error CS' not in log and 'Shader error' not in log
    assert (WORK/'Evidence/play-validation.txt').read_text().startswith('ENTRANCE_PLAY_PASS')
    for rel,digest in before.items():assert sha(ROOT/rel)==digest,'Concurrent user edit: '+rel
    assert sha(WORK/(PREFAB+'.meta'))==before[PREFAB+'.meta']
    names={'metal_office_desk','mid_century_lounge_chair','desk_lamp_arm_01','Stable warm desk lamp','Precisely aligned leather blotter','Ordered correspondence'}
    art_go=next(fid for fid,(kind,block) in old.items() if kind==1 and re.search(r'^  m_Name: Total Chaos Rebuild$',block,re.M))
    art_id=next(fid for fid,(kind,block) in old.items() if kind==4 and f'm_GameObject: {{fileID: {art_go}}}' in block)
    allowed_transforms=set()
    for fid,(kind,block) in old.items():
        if kind!=4 or 'stripped' in block.splitlines()[0]:continue
        go=re.search(r'm_GameObject: \{fileID: (-?\d+)',block)[1]
        name=re.search(r'^  m_Name: (.*)$',old[go][1],re.M)[1]
        if name in names and f'm_Father: {{fileID: {art_id}}}' in block:allowed_transforms.add(fid)
    assert len(allowed_transforms)==11
    layout_guid=re.search(r'^guid: (.*)$',(ROOT/'Assets/_Project/Global/Scripts/EntranceRebuild/EntranceDressingLayout.cs.meta').read_text(),re.M)[1]
    allowed_layout={fid for fid,(kind,block) in old.items() if kind==114 and 'guid: '+layout_guid in block}
    assert len(allowed_layout)==1
    changed=[]
    for fid,(kind,block) in old.items():
        current=new[fid][1]
        if kind==4:
            for field in ['m_LocalPosition','m_LocalRotation','m_LocalScale','m_Father']:
                pat=rf'^  {field}:.*$';a=re.search(pat,block,re.M);b=re.search(pat,current,re.M)
                if a and a[0]!=b[0]:assert fid in allowed_transforms and field in ['m_LocalPosition','m_LocalScale'],(fid,field)
        if re.sub(r'\s+',' ',block)!=re.sub(r'\s+',' ',current):
            assert fid in allowed_transforms|allowed_layout,'Unrelated component changed: '+fid
            changed.append(dict(fileID=fid,type=kind))
    data=re.sub(r'[ \t]+$','',text,flags=re.M).encode('utf-8');(ROOT/PREFAB).write_bytes(data)
    for p in (WORK/'Evidence/FurnitureScale').iterdir():shutil.copy2(p,OUT/p.name)
    shutil.copy2(WORK/'furniture-scale.log',OUT/'unity-scale.log');shutil.copy2(WORK/'furniture-verify.log',OUT/'unity-verify.log')
    shutil.copy2(WORK/'Evidence/play-validation.txt',OUT/'play-validation.txt')
    for rel,digest in before.items():
        if rel!=PREFAB:assert sha(ROOT/rel)==digest,'Untouched file changed: '+rel
    result=dict(status='PASS',factor=1.3,prefab=PREFAB,sha256=sha(ROOT/PREFAB),all_fileids_preserved=len(old),changed_components=changed,scene_and_player_preserved=True)
    (OUT/'installed.json').write_text(json.dumps(result,indent=2));print(json.dumps(result,indent=2))
