"""Snapshot this revision and synchronize the existing isolated Unity workspace."""
import hashlib
import json
import shutil
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
WORK = ROOT / '.utmp/CylinderDetailPreview'
BACKUP = ROOT / '.utmp/CylinderCanteenDoorsBefore'


def write_geometry():
    source = (ROOT / 'Tools/Cylinder/DetailRevision/CylinderDetailBuilder.cs').read_text()
    body = source[source.index('    static Mesh Persist('):]
    for name in ('Box', 'Cylinder', 'Pipe', 'Batch'):
        body = body.replace('static void ' + name + '(', 'public static void ' + name + '(')
    header = '''// Reuse the preceding revision's geometry algorithms with a separate output folder.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;
public static class CylinderCanteenGeometry
{
    const string Meshes = "Assets/_Project/Scenes/Cylinder/Meshes/CanteenDoors";
    public static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();
    static readonly Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();
    static int serial;
    static Vector3 V(float x,float y,float z) => new Vector3(x,y,z);
    public static Transform Group(Transform parent,string name,Vector3 position)
    { var t = new GameObject(name).transform; t.SetParent(parent,false); t.localPosition=position; return t; }
'''
    (ROOT / 'Tools/Cylinder/CanteenDoors/CylinderCanteenGeometry.cs').write_text(header + body)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    if BACKUP.exists():
        raise SystemExit('Revision already prepared; refusing to overwrite the backup')
    BACKUP.mkdir(parents=True)
    manifest = {}
    count = 0
    for source in (ROOT / 'Assets').rglob('*'):
        if not source.is_file():
            continue
        rel = source.relative_to(ROOT).as_posix()
        manifest[rel] = sha(source)
        target = WORK / rel
        if not target.exists() or sha(target) != manifest[rel]:
            target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, target)
            count += 1
    (BACKUP / 'manifest.json').write_text(json.dumps(manifest, indent=2))
    prefab = 'Assets/_Project/Scenes/Cylinder/Prefabs/Cylinder.prefab'
    shutil.copy2(ROOT / prefab, BACKUP / 'Cylinder.prefab')
    shutil.copy2(ROOT / (prefab + '.meta'), BACKUP / 'Cylinder.prefab.meta')
    write_geometry()
    print(f'Preserved {len(manifest)} files; synchronized {count} into the isolated workspace.')


if __name__ == '__main__':
    main()
