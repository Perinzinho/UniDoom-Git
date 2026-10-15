"""Small variation of existing lights; reuse the project's existing flicker runtime."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
WORK=ROOT/'.utmp/CylinderDetailPreview'
BACKUP=ROOT/'.utmp/CylinderAbandonedLightingBefore'
OUT=ROOT/'Tools/Cylinder/AbandonedLighting'
PREFAB='Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab'
SCRIPT='Assets/_Project/Global/Scripts/TotalChaos/TCFlickerLight.cs'
BLOCK=re.compile(r'(?ms)^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)')
WEAK={'5929003806968244892':26,'8913311444593308694':28,'8400577148571674640':38,'6563073049681176813':3,'8034186246090133953':3.5}
FLICKERS={
    '7406942453112340403':dict(go='3073672788693512132',amount=.18,speed=2.4,chance=.10,new=False),
    '9014101600000000001':dict(go='7733325252398632137',amount=.25,speed=3.1,chance=.14,new=True),
    '9014101600000000002':dict(go='2830659709601558837',amount=.20,speed=2.1,chance=.08,new=True),
}

def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()

def prepare():
    if BACKUP.exists():raise RuntimeError('Existing backup will not be overwritten')
    BACKUP.mkdir(parents=True)
    for suffix in ('','.meta'):shutil.copy2(ROOT/(PREFAB+suffix),BACKUP/('Cylinder.prefab'+suffix))
    shutil.copy2(ROOT/PREFAB,WORK/PREFAB)
    for suffix in ('','.meta'):shutil.copy2(ROOT/(SCRIPT+suffix),WORK/(SCRIPT+suffix))
    (OUT/'baseline.json').write_text(json.dumps(dict(prefab_sha256=sha(ROOT/PREFAB),meta_sha256=sha(ROOT/(PREFAB+'.meta')),runtime_sha256=sha(ROOT/SCRIPT)),indent=2))
    print('Current lighting preserved; isolated copy synchronized.')

def field(block,name,value):
    pattern=r'(?m)^  '+re.escape(name)+r': .*$(?:\n)?'
    line=f'  {name}: {value}\n'
    return re.sub(pattern,lambda _:line,block,count=1) if re.search(pattern,block) else block.rstrip()+'\n'+line

def candidate():
    original=(BACKUP/'Cylinder.prefab').read_text(encoding='utf-8-sig')
    blocks={m[2]:(m[1],m[0]) for m in BLOCK.finditer(original)}
    template=blocks['7406942453112340403'][1]
    modified={};new=[]
    for identifier,value in WEAK.items():
        assert blocks[identifier][0]=='108'
        modified[identifier]=field(blocks[identifier][1],'m_Intensity',value)
    for identifier,setting in FLICKERS.items():
        b=template if setting['new'] else blocks[identifier][1]
        if setting['new']:
            assert identifier not in blocks
            b=b.replace('&7406942453112340403','&'+identifier,1)
            b=field(b,'m_GameObject','{fileID: '+setting['go']+'}')
            go=setting['go'];kind,gameobject=blocks[go];assert kind=='1'
            component=re.search(r'(?ms)^  m_Component:\n(?:  - component:.*\n)+',gameobject)[0]
            modified[go]=gameobject.replace(component,component+'  - component: {fileID: '+identifier+'}\n',1)
        for name,value in [('m_Enabled',1),('flickerAmount',setting['amount']),('speed',setting['speed']),('dropoutChancePerSecond',setting['chance']),('synchronizeEmission',1)]:b=field(b,name,value)
        if setting['new']:new.append(b)
        else:modified[identifier]=b
    result=BLOCK.sub(lambda m:modified.get(m[2],m[0]),original)+''.join(new)
    final={m[2]:(m[1],m[0]) for m in BLOCK.finditer(result)}
    assert blocks.keys()<=final.keys() and len(final)==len(blocks)+2
    for identifier,(kind,b) in blocks.items():
        if identifier not in modified:assert final[identifier][1]==b
        elif kind=='108':
            assert re.sub(r'(?m)^  m_Intensity: .*\n','',final[identifier][1])==re.sub(r'(?m)^  m_Intensity: .*\n','',b)
        elif kind=='1':
            inserted=next(k for k,v in FLICKERS.items() if v['go']==identifier)
            assert final[identifier][1].replace('  - component: {fileID: '+inserted+'}\n','')==b
        else:assert identifier=='7406942453112340403'
    (WORK/PREFAB).write_text(result,encoding='utf-8',newline='\n')
    (OUT/'changes.json').write_text(json.dumps(dict(weak_lights=WEAK,flickers=FLICKERS,normal_lights=14,existing_fileIDs_preserved=len(blocks),added_flicker_components=2,changed_existing_blocks=len(modified),transforms_ambient_geometry_materials_collisions_and_gameplay_preserved=True),indent=2))
    print('Prepared: 14 steady normal lights, 5 weak lights, 3 independent flickers; positions and ambient unchanged.')

def install():
    baseline=json.loads((OUT/'baseline.json').read_text());evidence=WORK/'AbandonedLightingEvidence'
    for name in ('edit-validation.txt','play-validation.txt'):assert (evidence/name).read_text().startswith('PASS\n'),name
    assert sha(ROOT/PREFAB)==baseline['prefab_sha256'],'Concurrent prefab modification'
    assert sha(ROOT/(PREFAB+'.meta'))==baseline['meta_sha256']
    assert sha(ROOT/SCRIPT)==baseline['runtime_sha256'],'Shared flicker runtime changed'
    assert sha(WORK/PREFAB)==(evidence/'validated-sha256.txt').read_text().strip()
    shutil.copy2(WORK/PREFAB,ROOT/PREFAB)
    for p in evidence.iterdir():
        if p.is_file():shutil.copy2(p,OUT/p.name)
    (OUT/'installed.json').write_text(json.dumps(dict(only_asset_written=PREFAB,sha256=sha(ROOT/PREFAB),meta_and_shared_runtime_unchanged=True),indent=2))
    print('Installed prefab only; shared runtime and all other assets preserved.')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['prepare','candidate','install']);args=parser.parse_args();globals()[args.action]()
