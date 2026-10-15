"""Lighting-only prefab change. Preserve every other byte and all incoming IDs."""
import argparse
import hashlib
import json
import re
import shutil
from pathlib import Path

ROOT=Path(__file__).resolve().parents[3]
WORK=ROOT/'.utmp/CylinderDetailPreview'
BACKUP=ROOT/'.utmp/CylinderInteriorLightingBefore'
OUT=ROOT/'Tools/Cylinder/InteriorLighting'
PREFAB='Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab'
BLOCK=re.compile(r'(?ms)^--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=^--- !u!|\Z)')

def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()

def prepare():
    if BACKUP.exists(): raise RuntimeError('Backup exists; refusing to replace it')
    BACKUP.mkdir(parents=True)
    for suffix in ('','.meta'): shutil.copy2(ROOT/(PREFAB+suffix),BACKUP/('Cylinder.prefab'+suffix))
    shutil.copy2(ROOT/PREFAB,WORK/PREFAB)
    (OUT/'baseline.json').write_text(json.dumps(dict(prefab_sha256=sha(ROOT/PREFAB),meta_sha256=sha(ROOT/(PREFAB+'.meta'))),indent=2))
    print('Incoming prefab preserved; isolated copy synchronized.')

def candidate():
    source=(BACKUP/'Cylinder.prefab').read_text(encoding='utf-8-sig')
    names={m[2]:re.search(r'^  m_Name: (.*)',m[0],re.M)[1] for m in BLOCK.finditer(source) if m[1]=='1'}
    changes=[]
    def change(m):
        kind,identifier,block=m[1],m[2],m[0]
        result=block
        if kind=='108' and re.search(r'^  m_Enabled: 1$',block,re.M):
            intensity=float(re.search(r'^  m_Intensity: (.*)',block,re.M)[1])
            name=names[re.search(r'm_GameObject: \{fileID: (-?\d+)',block)[1]]
            target=None
            if intensity>0:
                if name=='Cold fluorescent spill':
                    target=(65,16)
                    result=re.sub(r'(?m)^  m_SpotAngle: .*$', '  m_SpotAngle: 145',result)
                    result=re.sub(r'(?m)^  m_InnerSpotAngle: .*$', '  m_InnerSpotAngle: 95',result)
                elif re.search(r'^  m_Type: 0$',block,re.M):
                    target=(60,20)
                    result=re.sub(r'(?m)^  m_Type: .*$', '  m_Type: 2',result)
                elif name=='Shielded desk light': target=(6,4.5)
                else: target=(7,9)
            if target:
                for field,value in zip(('m_Intensity','m_Range'),target):
                    result=re.sub(r'(?m)^  '+field+r': .*$',f'  {field}: {value}',result)
        elif kind=='114' and '  theme: cylinder\n' in block and '  ambient:' in block:
            result=re.sub(r'(?m)^  ambient: .*$', '  ambient: {r: 0.6, g: 0.65, b: 0.68, a: 1}',result)
        if result!=block:
            # Verify that only these three lighting properties can differ.
            strip=lambda b:re.sub(r'(?m)^  (?:m_Type|m_Intensity|m_Range|m_SpotAngle|m_InnerSpotAngle|ambient): .*\n','',b)
            assert strip(result)==strip(block)
            changes.append(dict(fileID=identifier,type=kind))
        return result
    result=BLOCK.sub(change,source)
    assert changes and sum(x['type']=='114' for x in changes)==1
    assert [(m[1],m[2]) for m in BLOCK.finditer(source)]==[(m[1],m[2]) for m in BLOCK.finditer(result)]
    (WORK/PREFAB).write_text(result,encoding='utf-8',newline='\n')
    (OUT/'changes.json').write_text(json.dumps(dict(changed_blocks=changes,allowed_fields=['Light.m_Type','Light.m_Intensity','Light.m_Range','Light.m_SpotAngle','Light.m_InnerSpotAngle','TCRegionAtmosphere.ambient'],all_fileIDs_and_other_fields_preserved=True),indent=2))
    print(f'Prepared {len(changes)-1} existing light adjustments and one ambient adjustment.')

def install():
    baseline=json.loads((OUT/'baseline.json').read_text())
    evidence=WORK/'InteriorLightingEvidence'
    assert (evidence/'validation.txt').read_text().startswith('PASS\n')
    assert sha(ROOT/PREFAB)==baseline['prefab_sha256'],'Concurrent user change to prefab'
    assert sha(ROOT/(PREFAB+'.meta'))==baseline['meta_sha256'],'Concurrent prefab GUID change'
    assert sha(WORK/PREFAB)==(evidence/'validated-sha256.txt').read_text().strip(),'Candidate changed after Unity validation'
    shutil.copy2(WORK/PREFAB,ROOT/PREFAB)
    for p in evidence.iterdir():
        if p.is_file(): shutil.copy2(p,OUT/p.name)
    (OUT/'installed.json').write_text(json.dumps(dict(only_project_asset_written=PREFAB,sha256=sha(ROOT/PREFAB),meta_preserved=True),indent=2))
    print('Installed lighting-only prefab; metadata and all other assets untouched by installer.')

if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('action',choices=['prepare','candidate','install']);args=parser.parse_args()
    globals()[args.action]()
