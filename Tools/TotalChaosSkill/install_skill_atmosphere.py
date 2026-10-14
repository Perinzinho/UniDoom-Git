"""Install only the approved atmosphere revision after isolated Unity validation."""
from pathlib import Path
import argparse
import hashlib
import json
import re
import shutil

ROOT=Path(__file__).resolve().parents[2]
PREVIEW=ROOT/'.utmp/BasementPreview'
BEFORE=ROOT/'.utmp/SkillAtmosphere-before'
TARGETS=[Path(p) for p in ('Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab',
    'Assets/prefabs/Scenario/Basement.prefab','Assets/Art/Basement/BasementFinalArena.prefab',
    'Assets/Art/Basement/BasementFinalSanctum.prefab','Assets/prefabs/Scenario/Cylinder.prefab')]
ART=Path('Assets/Art/SkillAtmosphere')


def blocks(text):
    return {m[2]:(int(m[1]),m[0]) for m in re.finditer(r'--- !u!(\d+) &(-?\d+)[^\n]*\n.*?(?=--- !u!|\Z)',text,re.S)}


def merge(original,generated):
    old,new=blocks(original),blocks(generated)
    assert set(old)<=set(new),'Original IDs removed'
    fields={1:['m_Component'],4:['m_Children'],23:['m_Materials'],33:['m_Mesh'],
            108:['m_Color','m_Intensity','m_Range','m_Shadows']}
    result=original[:original.index('--- !u!')]
    changed=0
    for fid,(kind,block) in old.items():
        assert new[fid][0]==kind
        initial=block
        for field in fields.get(kind,[]):
            pattern=rf'^  {field}:.*?(?=^  [A-Za-z_]|\Z)'
            a,b=re.search(pattern,block,re.M|re.S),re.search(pattern,new[fid][1],re.M|re.S)
            if a and b:block=block[:a.start()]+b[0]+block[a.end():]
        changed+=initial!=block
        result+=block
    result+=''.join(block for fid,(_,block) in new.items() if fid not in old)
    print(f'{len(old)} original blocks retained; {changed} adjusted; {len(new)-len(old)} new blocks.')
    return result.encode('utf-8')


def validate_log(name,markers):
    text=(ROOT/'.utmp'/name).read_text(encoding='utf-8',errors='replace')
    completion=[] if name=='Skill-playmode.log' else ['Exiting batchmode successfully']
    for marker in markers+completion:
        assert marker in text,f'{name}: missing {marker}'
    for error in ['Exception:','error CS','Render Graph Execution error']:
        assert error not in text,f'{name}: {error}'


def main():
    parser=argparse.ArgumentParser(description=__doc__);parser.add_argument('--install',action='store_true');args=parser.parse_args()
    validate_log('Skill-atmosphere-build.log',['SKILL_ATMOSPHERE_BUILD_OK','SKILL_ATMOSPHERE_VERIFIED'])
    if args.install:
        validate_log('Skill-atmosphere-verify.log',['SKILL_ATMOSPHERE_VERIFIED','SKILL_CAMERA_FX_OK Basement-Boss-Close'])
        validate_log('Skill-classroom.log',['CLASSROOM_ACTUAL_PLAYER_ROUTE_OK','CLASSROOM_DOOR_MOTION_VERIFIED','CLASSROOM_MAP_INHERITANCE_OK'])
        validate_log('Skill-basement.log',['BASEMENT_FINAL_VERIFIED','BASEMENT_FINAL_MAP_PLAYER_OK','BASEMENT_FINAL_NEIGHBOURS_OK'])
        validate_log('Skill-cylinder.log',['CYLINDER_DECAY_VERIFIED','CYLINDER_PLAYER_ROUTES_OK','CYLINDER_DOORS_OK','CYLINDER_ROOF_COVERAGE_OK'])
        validate_log('Skill-playmode.log',['SKILL_PLAYMODE_VERIFIED','SKILL_PLAYMODE_EXIT_RESTORES_ENVIRONMENT','SKILL_PLAYMODE_FLICKER_OK'])
        assert (PREVIEW/'Preview/Skill-playmode-exit-code.txt').read_text()=='0','Play-mode runner did not finish successfully'
    manifest=json.loads((BEFORE/'manifest.json').read_text(encoding='utf-8'))
    for path,digest in manifest.items():
        assert hashlib.sha256((ROOT/path).read_bytes()).hexdigest()==digest,f'Concurrent main-project edit: {path}'
    for path in ['Assets/Scenes/SampleScene.unity','Assets/prefabs/Player/Player.prefab','Assets/prefabs/Scenario/ClassRoom.prefab']:
        assert (PREVIEW/path).read_bytes()==(BEFORE/path).read_bytes(),f'Unrelated scene/player edit: {path}'
    merged={}
    for path in TARGETS:
        print(path);merged[path]=merge((BEFORE/path).read_text(encoding='utf-8-sig'),(PREVIEW/path).read_text(encoding='utf-8-sig'))
    if not args.install:
        for path,data in merged.items():(PREVIEW/path).write_bytes(data)
        print('Staged preserved prefabs. Run the four edit-mode checks and the play-mode check before --install.')
        return
    for path,data in merged.items():assert data==(PREVIEW/path).read_bytes(),f'Staged prefab changed: {path}'
    copies=[]
    for folder in [ART,Path('Assets/TotalChaos')]:
        for source in (PREVIEW/folder).rglob('*'):
            if source.is_file():
                if source.suffix=='.unity' or source.name.endswith('.unity.meta'):continue
                target=ROOT/source.relative_to(PREVIEW);assert not target.exists(),f'New asset already exists: {target}';copies.append((source,target))
        meta=PREVIEW/Path(str(folder)+'.meta')
        if meta.exists():copies.append((meta,ROOT/Path(str(folder)+'.meta')))
    # Only importer sampling/resolution settings change on the existing authored textures.
    for source in (PREVIEW/'Assets/Art/TotalChaosInspired/Textures').glob('*.png.meta'):
        target=ROOT/source.relative_to(PREVIEW)
        if source.read_bytes()!=target.read_bytes():copies.append((source,target))
    for source,target in copies:
        target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(source,target)
    for path,data in merged.items():(ROOT/path).write_bytes(data)
    output=ROOT/'Tools/TotalChaosSkill'
    for source in (PREVIEW/'Preview').glob('Skill-*.png'):shutil.copy2(source,output/source.name)
    shutil.copy2(PREVIEW/'Preview/Skill-atmosphere-validation.txt',output/'validation.txt')
    shutil.copy2(PREVIEW/'Preview/Skill-playmode-validation.txt',output/'playmode-validation.txt')
    print(f'Installed {len(copies)} asset/script files and the five verified prefabs.')


if __name__=='__main__':main()
