"""Remove serializer trailing whitespace without changing any YAML values."""
import hashlib
import json
import os
import re
from pathlib import Path

ROOT=Path(__file__).resolve().parents[2]
OUT=Path(__file__).resolve().parent
WORK=ROOT/'.utmp/EntranceRebuild'
manifest=OUT/'installed.json'
records=json.loads(manifest.read_text())
def sha(path):return hashlib.sha256(path.read_bytes()).hexdigest()
for record in records:
    assert sha(ROOT/record['path'])==record['sha256'],('Later edit; stop formatting',record['path'])
changed=[]
for record in records:
    path=ROOT/record['path'];data=path.read_bytes()
    if path.suffix not in ['.prefab','.unity']:continue
    if not (data.startswith(b'%YAML') or data.startswith(b'fileFormatVersion:')):continue
    formatted=re.sub(rb'[ \t]+(?=\r?$)',b'',data,flags=re.M)
    if data==formatted:continue
    # Ignore whitespace only when comparing: serialized field contents must agree exactly.
    assert [line.rstrip() for line in data.splitlines()]==formatted.splitlines()
    temporary=path.with_name(path.name+'.campus-format');temporary.write_bytes(formatted);os.replace(temporary,path)
    (WORK/record['path']).write_bytes(formatted)
    record['sha256']=sha(path);record['bytes']=path.stat().st_size;changed.append(record['path'])
manifest.write_text(json.dumps(records,indent=2))
(OUT/'formatting.json').write_text(json.dumps(dict(trailing_whitespace_only=True,files=changed),indent=2))
print('Normalized serializer whitespace in',len(changed),'files; all YAML values preserved.')
