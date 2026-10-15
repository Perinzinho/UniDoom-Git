"""Preserve the original prefab IDs and physics; install only the validated visual pass."""
from pathlib import Path
import re,json,hashlib,shutil,sys
ROOT=Path(__file__).resolve().parents[3]
WORK=ROOT/'.utmp/BasementRitualExpansion'
PREVIEW=ROOT/'.utmp/BasementPreview'
ART='Assets/Art/Basement/RitualExpansion'
TARGETS=['Assets/prefabs/Scenario/Basement.prefab','Assets/Art/Basement/BasementFinalArena.prefab','Assets/Art/Basement/BasementFinalSanctum.prefab']
def sha(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(s):return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n[\s\S]*?(?=--- !u!|\Z)',s)}
def fields(s):return {m[1]:m[0] for m in re.finditer(r'^  (\w+):[^\n]*(?:\n(?!  \w+:)[^\n]*)*',s,re.M)}
def write(p,data):
    p.parent.mkdir(parents=True,exist_ok=True)
    with p.open('r+b' if p.exists() else 'wb') as f:f.write(data);f.truncate()
if '--install' not in sys.argv:
    report=[]
    for target in TARGETS:
        original=(WORK/'before'/target).read_text(encoding='utf-8-sig');generated=(PREVIEW/target).read_text(encoding='utf-8-sig')
        old,new=blocks(original),blocks(generated);output=original[:original.index('--- !u!')];changed=[]
        for ident,(kind,s) in old.items():
            assert ident in new and new[ident][0]==kind,('Existing object removed',target,ident)
            a,b=fields(s),fields(new[ident][1])
            for field in {4:{'m_Children'},23:{'m_Enabled','m_Materials'}}.get(kind,set()):
                if a.get(field)==b.get(field):continue
                assert field in a and field in b
                if kind==4:
                    x=re.findall(r'fileID: (-?\d+)',a[field]);y=re.findall(r'fileID: (-?\d+)',b[field])
                    assert y[:len(x)]==x and all(i not in old for i in y[len(x):]),'Existing objects reparented'
                s=s.replace(a[field],b[field],1);changed.append([ident,kind,field])
            output+=s
        for ident,(kind,s) in new.items():
            if ident not in old:
                assert kind in {1,4,23,33,108,114},('Unexpected new component',kind)
                output+=s
        staged=blocks(output)
        assert len(staged)==len(new),'Unexpected prefab serialization'
        assert all(staged[i][1]==s for i,(k,s) in old.items() if k in {54,64,65,114,136,143,195,1001}), 'Original gameplay, atmosphere or physics modified'
        write(PREVIEW/target,output.encode());write(WORK/'staged'/target,output.encode())
        report.append(dict(target=target,old_ids=len(old),new_blocks=len(new)-len(old),changed=changed,physics_preserved=True,gameplay_preserved=True))
        print(target,':',len(new)-len(old),'new visual blocks;',len(changed),'changed child lists/visibility fields')
    (WORK/'staging-report.json').write_text(json.dumps(report,indent=2))
else:
    runs=[('build.log','BASEMENT_EXPANSION_BUILD_OK'),('verify.log','BASEMENT_EXPANSION_VERIFIED'),('play.log','BASEMENT_SURFACE_PLAYMODE_OK')]
    runs.extend((name,marker) for name,marker in [('refine.log','BASEMENT_EXPANSION_REFINED'),('passages.log','BASEMENT_EXPANSION_PASSAGES_FIXED')] if (WORK/name).exists())
    for name,marker in runs:
        log=(WORK/name).read_text(encoding='utf-8',errors='replace');assert marker in log,name+' incomplete'
        assert ('Shutdown.' in log if name=='play.log' else 'Exiting batchmode successfully' in log),name+' did not finish'
        for error in ['error CS','Exception:','Shader error in','Render Graph Execution error']:assert error not in log,(name,error)
    baseline=json.loads((WORK/'baseline.json').read_text())
    for target in TARGETS:
        assert sha(ROOT/target)==baseline[target],'Concurrent prefab edit: '+target
        assert sha(PREVIEW/target)==sha(WORK/'staged'/target),'Validated prefab changed: '+target
    assert not (ROOT/ART).exists(),'Output exists'
    shutil.copytree(PREVIEW/ART,ROOT/ART);shutil.copy2(PREVIEW/(ART+'.meta'),ROOT/(ART+'.meta'))
    for target in TARGETS:write(ROOT/target,(WORK/'staged'/target).read_bytes())
    docs=ROOT/'Tools/Basement/RitualExpansion'
    for prefix in ['BeforeExpansion','VerifiedExpansion']:
        for p in (PREVIEW/'Preview/BasementSurface').glob(prefix+'-*.png'):shutil.copy2(p,docs/p.name)
    shutil.copy2(PREVIEW/'Preview/Expansion-validation.txt',docs/'validation.txt')
    shutil.copy2(PREVIEW/'Preview/BasementSurface/runtime-validation.txt',docs/'runtime-validation.txt')
    shutil.copy2(WORK/'staging-report.json',docs/'staging-report.json')
    for target in TARGETS:assert sha(ROOT/target)==sha(PREVIEW/target),'Installed prefab mismatch'
    print('Installed: three validated prefabs, new art, comparison captures and validation reports. Original IDs, gameplay and physics retained.')
