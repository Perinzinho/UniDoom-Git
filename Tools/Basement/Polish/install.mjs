import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const root=process.cwd(),work=path.join(root,'.utmp/BasementPolish'),preview=path.join(root,'.utmp/BasementPreview');
const target='Assets/prefabs/Scenario/Basement.prefab',art='Assets/Art/Basement/ArenaPolish';
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const assert=(v,message)=>{if(!v)throw Error(message);};
function blocks(text){return new Map([...text.matchAll(/--- !u!(\d+) &(-?\d+)[^\n]*\n[\s\S]*?(?=--- !u!|$)/g)].map(m=>[m[2],{kind:+m[1],text:m[0]}]));}
function field(text,name){return text.match(new RegExp('^  '+name+':[\\s\\S]*?(?=^  [A-Za-z_]|$(?![\\s\\S]))','m'))?.[0];}
function replace(text,source,name){const a=field(text,name),b=field(source,name);assert(a&&b,'Missing '+name);return text.replace(a,b);}
function merge(){
 const original=fs.readFileSync(path.join(work,'before',target),'utf8').replace(/^\uFEFF/,''),generated=fs.readFileSync(path.join(preview,target),'utf8').replace(/^\uFEFF/,'');
 const old=blocks(original),next=blocks(generated);let output=original.slice(0,original.indexOf('--- !u!')),changed=0;
 for(const [id,b] of old){
  const n=next.get(id);assert(n&&b.kind===n.kind,'Existing object removed '+id);let text=b.text;
  if(b.kind===4 && field(text,'m_Children')!==field(n.text,'m_Children')){
   const a=[...field(text,'m_Children').matchAll(/fileID: (-?\d+)/g)].map(m=>m[1]),c=[...field(n.text,'m_Children').matchAll(/fileID: (-?\d+)/g)].map(m=>m[1]);
   assert(a.every((id,i)=>c[i]===id),'Existing children reordered '+id);
   assert(c.slice(a.length).every(id=>!old.has(id)),'Existing transform reparented');text=replace(text,n.text,'m_Children');
  }
  if(b.kind===23)text=replace(text,n.text,'m_Enabled');
  changed+=text!==b.text;output+=text;
 }
 for(const [id,b]of next)if(!old.has(id)){assert([1,4,23,33,65,108].includes(b.kind),'Unexpected added component '+b.kind);output+=b.text;}
 assert(blocks(output).size===next.size,'Block count mismatch');
 fs.writeFileSync(path.join(preview,target),output);fs.writeFileSync(path.join(work,'staged.prefab'),output);
 console.log('Preserved',old.size,'existing IDs;',changed,'visual/child-list blocks updated;',next.size-old.size,'added blocks');
}
if(!process.argv.includes('--install'))merge();
else{
 for(const [name,marker]of [['build.log','BASEMENT_POLISH_BUILD_OK'],['verify.log','BASEMENT_POLISH_VERIFIED']]){
  const log=fs.readFileSync(path.join(work,name),'utf8');assert(log.includes(marker)&&log.includes('Exiting batchmode successfully'),'Missing success marker '+name);
  for(const error of ['error CS','Exception:','Shader error in','Render Graph Execution error'])assert(!log.includes(error),error+' in '+name);
 }
 assert(fs.readFileSync(path.join(preview,target),'utf8')===fs.readFileSync(path.join(work,'staged.prefab'),'utf8'),'Validated prefab changed after staging');
 const baseline=JSON.parse(fs.readFileSync(path.join(work,'baseline.json'),'utf8'));
 for(const[p,sha]of Object.entries(baseline))assert(hash(path.join(root,p))===sha,'Concurrent main-project edit: '+p);
 assert(!fs.existsSync(path.join(root,art)),'Output directory already exists');
 fs.cpSync(path.join(preview,art),path.join(root,art),{recursive:true});fs.copyFileSync(path.join(preview,art+'.meta'),path.join(root,art+'.meta'));
 fs.copyFileSync(path.join(work,'staged.prefab'),path.join(root,target));
 const docs=path.join(root,'Tools/Basement/Polish');
 for(const name of fs.readdirSync(path.join(preview,'Preview/BasementSurface')))
  if(name.startsWith('Current-')||name.startsWith('FinalPolish-'))fs.copyFileSync(path.join(preview,'Preview/BasementSurface',name),path.join(docs,name));
 fs.copyFileSync(path.join(preview,'Preview/Polish/validation.txt'),path.join(docs,'validation.txt'));
 console.log('Installed validated prefab and new CC0 assets. Existing scene, player, gameplay and unrelated assets preserved. Backup: .utmp/BasementPolish/before');
}
