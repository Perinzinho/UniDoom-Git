import fs from 'node:fs';
import path from 'node:path';
import crypto from 'node:crypto';
const work='.utmp/BasementSymbol',preview='.utmp/BasementPreview';
const prefab='Assets/prefabs/Scenario/Basement.prefab';
const art='Assets/Art/Basement/ArenaPolish';
const texture=art+'/Textures/AltarReferenceSeal.png',material=art+'/Materials/AltarReferenceSeal.mat';
const source='C:/Users/perin/.codex/generated_images/01a13cef-69c8-7731-b208-80760011f62b/exec-fad1d6b9-0be2-4574-a6ba-c55ae7c4e4cc.png';
const digest=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const ensure=(x,m)=>{if(!x)throw Error(m);};
if(process.argv.includes('--install')){
 const manifest=JSON.parse(fs.readFileSync(work+'/manifest.json'));
 ensure(digest(prefab)===manifest.prefabBefore,'Concurrent prefab change');
 const log=fs.readFileSync(work+'/preview.log','utf8');
 ensure(log.includes('BASEMENT_SYMBOL_VERIFIED')&&log.includes('Exiting batchmode successfully'),'Preview not validated');
 for(const error of ['Exception:','error CS','Shader error in','Render Graph Execution error'])ensure(!log.includes(error),error);
 ensure(digest(preview+'/'+prefab)===manifest.prefabAfter,'Validated prefab changed');
 for(const p of [texture,texture+'.meta',material,material+'.meta']){
  ensure(!fs.existsSync(p),'Destination already exists: '+p);fs.copyFileSync(preview+'/'+p,p);
 }
 fs.copyFileSync(preview+'/'+prefab,prefab);
 for(const n of ['Altar-NewSymbol.png','Altar-NewSymbol-Wide.png'])fs.copyFileSync(preview+'/Preview/BasementSurface/'+n,'Tools/Basement/Polish/'+n);
 console.log('Installed new altar symbol; original material and texture retained.');
 process.exit(0);
}
ensure(!fs.existsSync(work+'/manifest.json'),'Existing symbol snapshot retained');
fs.mkdirSync(work,{recursive:true});
fs.copyFileSync(prefab,work+'/Basement-before.prefab');
const tg=crypto.randomBytes(16).toString('hex'),mg=crypto.randomBytes(16).toString('hex');
fs.mkdirSync(preview+'/'+path.dirname(texture),{recursive:true});
fs.copyFileSync(source,preview+'/'+texture);
let meta=fs.readFileSync('Assets/Art/Basement/Textures/Seal.png.meta','utf8');
meta=meta.replace(/^guid: \w+/m,'guid: '+tg).replace('filterMode: 0','filterMode: 2').replace('aniso: 1','aniso: 4')
 .replaceAll(/wrap([UVW]): 0/g,'wrap$1: 1').replace('nPOTScale: 1','nPOTScale: 0').replace('alphaIsTransparency: 0','alphaIsTransparency: 1')
 .replace('mipMapsPreserveCoverage: 0','mipMapsPreserveCoverage: 1').replace('alphaTestReferenceValue: 0.5','alphaTestReferenceValue: 0.08');
fs.writeFileSync(preview+'/'+texture+'.meta',meta);
let mat=fs.readFileSync('Assets/Art/SkillAtmosphere/Materials/Basement/Seal.mat','utf8').replace(/\r\n/g,'\n');
mat=mat.replace('m_Name: Seal','m_Name: AltarReferenceSeal').replaceAll('79f7320685fe529419e45ab6435d21b6',tg)
 .replace('  m_ValidKeywords:\n','  m_ValidKeywords:\n  - _ALPHATEST_ON\n').replace('m_CustomRenderQueue: -1','m_CustomRenderQueue: 2450')
 .replace('RenderType: Opaque','RenderType: TransparentCutout').replace('_AlphaClip: 0','_AlphaClip: 1').replace('_AlphaToMask: 0','_AlphaToMask: 1').replace('_Cutoff: 0.5','_Cutoff: 0.08')
 .replace('_BaseColor: {r: 0.875, g: 0.875, b: 0.875, a: 1}','_BaseColor: {r: 1, g: 1, b: 1, a: 1}')
 .replace('_EmissionColor: {r: 0.21176472, g: 0.04705883, b: 0.04705883, a: 0.6}','_EmissionColor: {r: 0.55, g: 0.12, b: 0.1, a: 1}');
// The front face UVs reverse both axes. Compensate and fit the square emblem.
const aspect=8.1/6.815;
mat=mat.replaceAll('m_Scale: {x: 1, y: 1}',`m_Scale: {x: ${-aspect}, y: -1}`).replaceAll('m_Offset: {x: 0, y: 0}',`m_Offset: {x: ${(1+aspect)/2}, y: 1}`);
fs.writeFileSync(preview+'/'+material,mat);
fs.writeFileSync(preview+'/'+material+'.meta',`fileFormatVersion: 2\nguid: ${mg}\nNativeFormatImporter:\n  externalObjects: {}\n  mainObjectFileID: 2100000\n  userData: \n  assetBundleName: \n  assetBundleVariant: \n`);
const before=fs.readFileSync(prefab,'utf8');const old='b6a451e4b4a8527479e8d89790091562';
ensure(before.split(old).length===2,'Expected exactly one altar material reference');
const after=before.replace(old,mg);fs.writeFileSync(preview+'/'+prefab,after);
fs.copyFileSync('Assets/Scenes/SampleScene.unity',preview+'/Assets/Scenes/SampleScene.unity');
fs.writeFileSync(work+'/manifest.json',JSON.stringify({prefabBefore:digest(prefab),prefabAfter:digest(preview+'/'+prefab),textureGuid:tg,materialGuid:mg,source,description:'User-provided reference, background extraction with built-in imagegen; all other prefab bytes preserved.'},null,2));
console.log('Staged symbol texture and material; exactly one prefab material GUID changed.');
