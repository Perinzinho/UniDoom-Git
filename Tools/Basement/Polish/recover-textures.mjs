import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
import {spawnSync} from 'node:child_process';
const preview='.utmp/BasementPreview',art=preview+'/Assets/Art/Basement/ArenaPolish';
if(fs.existsSync(art+'/recovered-textures.json')){console.log('Existing recovery and provenance retained.');process.exit(0);}
const current=new Set(),materials=[];
function walk(dir){for(const f of fs.readdirSync(dir,{withFileTypes:true})){const p=path.join(dir,f.name);if(f.isDirectory())walk(p);else if(f.name.endsWith('.meta'))current.add(fs.readFileSync(p,'utf8').match(/^guid: (\w+)/m)?.[1]);else if(f.name.endsWith('.mat')&&p.replaceAll('\\','/').includes('/Art/Basement/'))materials.push(p);}}
walk(preview+'/Assets');
const missing=new Set();
for(const p of materials)for(const m of fs.readFileSync(p,'utf8').matchAll(/m_Texture: \{fileID: [^,]+, guid: (\w+)/g))if(!current.has(m[1]))missing.add(m[1]);
const files=spawnSync('git',['ls-files','Assets/**/*.png.meta','Assets/**/*.jpg.meta'],{encoding:'utf8'}).stdout.trim().split('\n');
const batch=spawnSync('git',['cat-file','--batch'],{input:files.map(p=>'HEAD:'+p).join('\n')+'\n',maxBuffer:20*1024*1024}).stdout;
let offset=0;const manifest=[];
fs.mkdirSync(art+'/RecoveredTextures',{recursive:true});
for(const p of files){
 const end=batch.indexOf(10,offset);const header=batch.subarray(offset,end).toString();offset=end+1;
 if(header.endsWith(' missing'))continue;
 const size=+header.split(' ').at(-1),meta=batch.subarray(offset,offset+size).toString('utf8');offset+=size+1;
 const guid=meta.match(/^guid: (\w+)/m)?.[1];if(!missing.has(guid))continue;
 const source=p.slice(0,-5);if(!fs.existsSync(source))throw Error('Missing source '+source);
 const name=path.basename(path.dirname(source))+'_'+path.basename(source,path.extname(source))+'_'+guid.slice(0,8)+path.extname(source);
 const dest=art+'/RecoveredTextures/'+name;
 fs.copyFileSync(source,dest);fs.writeFileSync(dest+'.meta',meta);
 const authored=source.startsWith('Assets/Art/TotalChaosInspired/');
 const library=authored?'Tools/TotalChaosStyle/README.md':source.startsWith('Assets/Art/Basement/DetailPolish/')?'Assets/Art/Basement/DetailPolish/asset-sources.json':source.startsWith('Assets/Art/ClassRoom/Surface/')?'Assets/Art/ClassRoom/Surface/texture-sources.json':source.startsWith('Assets/Art/Basement/ArenaHorror/')?'Tools/Basement/Horror/README.md':'Assets/Art/Basement/Surface/texture-sources.json';
 manifest.push({source,recoveredFile:'RecoveredTextures/'+name,restoredGuid:guid,sha256:crypto.createHash('sha256').update(fs.readFileSync(source)).digest('hex'),license:authored?'Existing project asset; license unchanged':'CC0-1.0',provenance:library});
 missing.delete(guid);
}
if(missing.size)throw Error('Unresolved texture GUIDs: '+[...missing].join(','));
fs.writeFileSync(art+'/recovered-textures.json',JSON.stringify(manifest,null,2));
console.log('Recovered',manifest.length,'missing texture references without changing existing metadata.');
