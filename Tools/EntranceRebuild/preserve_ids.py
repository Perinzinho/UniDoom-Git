"""Restore two preexisting stripped transform IDs remapped by Unity's prefab save."""
import re
from pathlib import Path
from install import ROOT,WORK,PREFABS,blocks

def key(block):
    source=re.search(r'  m_CorrespondingSourceObject:.*',block)
    instance=re.search(r'  m_PrefabInstance:.*',block)
    return (source[0],instance[0]) if source and instance else None

def main():
    for rel in PREFABS:
        original=blocks((ROOT/'Tools/EntranceRebuild/Before'/rel).read_text(encoding='utf-8'))
        path=WORK/rel;text=path.read_text(encoding='utf-8');current=blocks(text)
        for old_id in original.keys()-current.keys():
            kind,old=original[old_id]
            assert kind==4 and 'stripped' in old.splitlines()[0],(rel,old_id)
            matches=[fid for fid,(kind,b) in current.items() if kind==4 and key(b)==key(old)]
            assert len(matches)==1,(rel,old_id,matches)
            new_id=matches[0]
            text=text.replace(f'--- !u!4 &{new_id} stripped',f'--- !u!4 &{old_id} stripped')
            text=re.sub(rf'fileID: {new_id}(?=[,}}])',f'fileID: {old_id}',text)
            print(Path(rel).name+': restored stripped transform '+old_id)
        assert original.keys()<=blocks(text).keys()
        path.write_text(text,encoding='utf-8',newline='\n')

if __name__=='__main__':main()
