"""Stage only intentional visual changes, then install the verified result."""
from pathlib import Path
import re, json, hashlib, shutil, sys
ROOT=Path(__file__).resolve().parents[3]
PREVIEW=ROOT/'.utmp/BasementPreview'
WORK=ROOT/'.utmp/BasementRitual'
TARGET='Assets/prefabs/Scenario/Basement.prefab'
ART='Assets/Art/Basement/Ritual'
def sha(p): return hashlib.sha256(p.read_bytes()).hexdigest()
def blocks(text):
    return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n[\s\S]*?(?=--- !u!|\Z)',text)}
def fields(text):
    return {m[1]:m[0] for m in re.finditer(r'^  (\w+):[^\n]*(?:\n(?!  \w+:)[^\n]*)*',text,re.M)}
def write(p,data):
    p.parent.mkdir(parents=True,exist_ok=True)
    with p.open('r+b' if p.exists() else 'wb') as out: out.write(data);out.truncate()

if '--install' not in sys.argv:
    oldtext=(WORK/'before'/TARGET).read_text(encoding='utf-8-sig')
    generated=(PREVIEW/TARGET).read_text(encoding='utf-8-sig')
    old,new=blocks(oldtext),blocks(generated)
    result=oldtext[:oldtext.index('--- !u!')];changes=[]
    allowed={4:{'m_Children'},23:{'m_Materials','m_Enabled'},108:{'m_Color','m_Intensity','m_Range','m_Shadows'},
             114:{'ambient','fogColor','fogDensity','sharedProfile'},1001:{'m_Modification'}}
    for ident,(kind,text) in old.items():
        assert ident in new and kind==new[ident][0],('Removed existing object',ident)
        nexttext=new[ident][1]
        before,after=fields(text),fields(nexttext)
        if kind==114:
            go=re.search(r'm_GameObject: \{fileID: (-?\d+)',text)
            if not go or 'm_Name: Boss skill atmosphere\n' not in old.get(go[1],(0,''))[1]:
                result+=text
                continue
        for field in allowed.get(kind,set()):
            if before.get(field)!=after.get(field):
                assert field in before and field in after,(ident,field)
                if kind==4:
                    a=re.findall(r'fileID: (-?\d+)',before[field]);b=re.findall(r'fileID: (-?\d+)',after[field])
                    assert b[:len(a)]==a and all(x not in old for x in b[len(a):]),'Existing children reordered'
                if kind==1001:
                    def records(value):
                        return {(m[1],m[2],m[3]):(m[4],m[5]) for m in re.finditer(r'- target: \{fileID: (-?\d+), guid: (\w+), type: \d+\}\n      propertyPath: ([^\n]+)\n      value: ([^\n]*)\n      objectReference: ([^\n]+)',value)}
                    a,b=records(before[field]),records(after[field])
                    assert all(k in b for k in a),'Existing nested override removed'
                    permitted={'m_Materials.Array.data[0]','m_Color.r','m_Color.g','m_Color.b','m_Enabled','m_Intensity','m_Range','m_Shadows.m_Type'}
                    assert all(k[2].strip("'") in permitted for k,v in b.items() if a.get(k)!=v),'Nonvisual nested override'
                text=text.replace(before[field],after[field],1);changes.append((ident,kind,field))
        result+=text
    for ident,(kind,text) in new.items():
        if ident not in old:
            assert kind in {1,4,23,33,108,114},('Unexpected added component',kind)
            result+=text
    assert len(blocks(result))==len(new)
    staged=blocks(result)
    physics=sum(kind in {65,64,54,136,143,195} for kind,text in old.values())
    assert all(staged[ident][1]==text for ident,(kind,text) in old.items() if kind in {65,64,54,136,143,195}),'Physics changed'
    write(PREVIEW/TARGET,result.encode('utf-8'));write(WORK/'staged.prefab',result.encode('utf-8'))
    (WORK/'changes.json').write_text(json.dumps(changes,indent=2))
    print('Staged:',len(old),'preserved object IDs;',len(new)-len(old),'new blocks;',len(changes),'visual fields updated')
    print('Preserved',physics,'original collider/rigidbody blocks; nested overrides restricted to materials and lights.')
else:
    for name,marker in [('build.log','BASEMENT_RITUAL_BUILD_OK'),('tune.log','BASEMENT_RITUAL_TUNED'),('verify.log','BASEMENT_RITUAL_VERIFIED'),('play.log','BASEMENT_SURFACE_PLAYMODE_OK')]:
        log=(WORK/name).read_text(encoding='utf-8',errors='replace')
        assert marker in log,name+' incomplete'
        # The asynchronous Play Mode runner exits through EditorApplication.Exit.
        assert ('Input System module state changed to: Shutdown.' in log if name=='play.log' else 'Exiting batchmode successfully' in log),name+' did not shut down cleanly'
        for error in ['error CS','Exception:','Shader error in','Render Graph Execution error']:assert error not in log,(name,error)
    assert (PREVIEW/TARGET).read_bytes()==(WORK/'staged.prefab').read_bytes(),'Validated prefab changed'
    baseline=json.loads((WORK/'baseline.json').read_text())
    for rel,digest in baseline.items():assert sha(ROOT/rel)==digest,'Concurrent project edit: '+rel
    assert not (ROOT/ART).exists(),'Destination already exists'
    shutil.copytree(PREVIEW/ART,ROOT/ART)
    shutil.copy2(PREVIEW/(ART+'.meta'),ROOT/(ART+'.meta'))
    write(ROOT/TARGET,(WORK/'staged.prefab').read_bytes())
    docs=ROOT/'Tools/Basement/Ritual'
    for p in (PREVIEW/'Preview/BasementSurface').glob('*Ritual*.png'):
        if p.name.startswith(('BeforeRitual-','VerifiedRitual-')):shutil.copy2(p,docs/p.name)
    shutil.copy2(PREVIEW/'Preview/Ritual-validation.txt',docs/'validation.txt')
    shutil.copy2(PREVIEW/'Preview/BasementSurface/runtime-validation.txt',docs/'runtime-validation.txt')
    untouched=sum(sha(ROOT/r)==digest for r,digest in baseline.items() if r!=TARGET)
    print('Installed validated ritual room;',untouched,'other existing project files byte-identical.')
