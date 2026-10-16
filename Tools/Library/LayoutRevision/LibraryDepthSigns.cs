using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object=UnityEngine.Object;

// Bake existing font glyphs to persistent cutout signs, which obey world depth.
public static class LibraryDepthSigns
{
    const string Art="Assets/_Project/Scenes/Library";
    public static void Bake(Transform art)
    {
        string texDir=Art+"/Textures/LayoutRevision/Signs",meshDir=Art+"/Meshes/LayoutRevision",matDir=Art+"/Materials/LayoutRevision";
        foreach(var p in new[]{texDir,meshDir,matDir})Directory.CreateDirectory(p);AssetDatabase.Refresh();
        var quad=new Mesh{name="DepthSignQuad"};quad.vertices=new[]{new Vector3(-.5f,-.5f,0),new Vector3(.5f,-.5f,0),new Vector3(.5f,.5f,0),new Vector3(-.5f,.5f,0)};quad.uv=new[]{Vector2.zero,Vector2.right,Vector2.one,Vector2.up};quad.triangles=new[]{0,2,1,0,3,2};quad.RecalculateNormals();quad.RecalculateTangents();AssetDatabase.CreateAsset(quad,meshDir+"/DepthSignQuad.asset");
        foreach(var text in art.GetComponentsInChildren<TextMesh>(true).Where(t=>t.GetComponent<MeshRenderer>().enabled).ToArray())
        {
            string key;using(var sha=SHA256.Create())key=BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(text.text+text.color.ToString()+text.characterSize))).Replace("-","").Substring(0,16);
            string png=texDir+"/"+key+".png",matPath=matDir+"/Sign_"+key+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(matPath);
            var original=text.GetComponent<MeshRenderer>();var bounds=original.localBounds;
            if(!material)
            {
                var copy=Object.Instantiate(text.gameObject);copy.name="Temporary glyph capture";copy.transform.SetParent(null);copy.transform.position=Vector3.zero;copy.transform.rotation=Quaternion.identity;copy.transform.localScale=Vector3.one;copy.layer=31;
                var camera=new GameObject("Temporary glyph camera").AddComponent<Camera>();camera.cullingMask=1<<31;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;camera.orthographic=true;camera.nearClipPlane=.01f;camera.farClipPlane=10;
                camera.gameObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing=false;
                var font=copy.GetComponent<TextMesh>();font.font.RequestCharactersInTexture(font.text,font.fontSize,font.fontStyle);
                var b=copy.GetComponent<MeshRenderer>().bounds;float aspect=Mathf.Clamp(b.size.x/Mathf.Max(.001f,b.size.y),.15f,12);
                int height=256,width=Mathf.Clamp(Mathf.RoundToInt(height*aspect),64,2048);camera.aspect=(float)width/height;camera.orthographicSize=Mathf.Max(b.size.y*.53f,b.size.x*.53f/camera.aspect);camera.transform.position=new Vector3(b.center.x,b.center.y,-3);
                var rt=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32);camera.targetTexture=rt;for(int i=0;i<3;i++)camera.Render();
                var texture=new Texture2D(width,height,TextureFormat.RGBA32,false);RenderTexture.active=rt;texture.ReadPixels(new Rect(0,0,width,height),0,0);texture.Apply();File.WriteAllBytes(png,texture.EncodeToPNG());RenderTexture.active=null;camera.targetTexture=null;
                Object.DestroyImmediate(texture);Object.DestroyImmediate(rt);Object.DestroyImmediate(camera.gameObject);Object.DestroyImmediate(copy);
                AssetDatabase.ImportAsset(png);var importer=(TextureImporter)AssetImporter.GetAtPath(png);importer.sRGBTexture=true;importer.alphaIsTransparency=true;importer.mipmapEnabled=true;importer.maxTextureSize=2048;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
                material=new Material(Shader.Find("UniDoom/TotalChaosInspiredSurface")){name="Sign_"+key};var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>(png);material.SetTexture("_BaseMap",atlas);material.SetTexture("_EmissionMap",atlas);material.SetColor("_BaseColor",Color.white);material.SetColor("_EmissionColor",Color.white);material.SetFloat("_AlphaClip",1);material.SetFloat("_Cutoff",.25f);material.SetFloat("_Cull",2);material.EnableKeyword("_ALPHATEST_ON");material.EnableKeyword("_EMISSION");material.renderQueue=2450;material.SetOverrideTag("RenderType","TransparentCutout");AssetDatabase.CreateAsset(material,matPath);
            }
            var g=new GameObject("Depth tested sign lettering");g.transform.SetParent(text.transform,false);g.transform.localPosition=bounds.center;g.transform.localScale=new Vector3(bounds.size.x*1.06f,bounds.size.y*1.06f,1);g.AddComponent<MeshFilter>().sharedMesh=quad;var renderer=g.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;renderer.receiveShadows=false;original.enabled=false;
        }
    }
}
