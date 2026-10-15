import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=process.cwd(), work=path.join(root,'.utmp/BasementPolish');
const preview=path.join(root,'.utmp/BasementPreview');
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
if(fs.existsSync(path.join(work,'baseline.json')))throw Error('Snapshot already exists');
const manifest={};
function scan(dir){for(const e of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,e.name);if(e.isDirectory())scan(p);else manifest[path.relative(root,p).replaceAll('\\','/')]=hash(p);}}
for(const dir of ['Assets','Packages','ProjectSettings']){scan(path.join(root,dir));fs.cpSync(path.join(root,dir),path.join(preview,dir),{recursive:true});}
for(const p of ['Assets/prefabs/Scenario/Basement.prefab','Assets/Art/Basement/BasementFinalArena.prefab','Assets/Art/Basement/BasementFinalSanctum.prefab']){const dest=path.join(work,'before',p);fs.mkdirSync(path.dirname(dest),{recursive:true});fs.copyFileSync(path.join(root,p),dest);}
fs.writeFileSync(path.join(work,'baseline.json'),JSON.stringify(manifest,null,2));
// The main Editor is closed. Copy its import cache; never share a writable Library.
fs.cpSync(path.join(root,'Library'),path.join(preview,'Library'),{recursive:true,filter:p=>!['Search','Bee','BurstCache'].includes(path.basename(p))});
fs.mkdirSync(path.join(preview,'Assets/Editor'),{recursive:true});
for(const file of ['BasementBuilder.cs','BasementFinalBuilder.cs','BasementFinalArt.cs','BasementFinalVerifier.cs'])fs.copyFileSync(path.join(root,'Tools/Basement',file),path.join(preview,'Assets/Editor',file));
fs.copyFileSync(path.join(root,'Tools/Basement/Surface/BasementSurfaceBuilder.cs'),path.join(preview,'Assets/Editor/BasementSurfaceBuilder.cs'));
console.log('Snapshot and isolated project ready:',Object.keys(manifest).length,'files');
