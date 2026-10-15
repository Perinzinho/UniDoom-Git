using System;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object=UnityEngine.Object;

public static class BasementSymbolPreview
{
    static Vector3 V(float x,float y,float z)=>new Vector3(x,y,z);
    public static void Run()
    {
        BasementSurfaceBuilder.RequireIsolated();
        const string art="Assets/Art/Basement/ArenaPolish";
        var mat=AssetDatabase.LoadAssetAtPath<Material>(art+"/Materials/AltarReferenceSeal.mat");
        var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/Textures/AltarReferenceSeal.png");
        if(!mat || !texture || mat.mainTexture!=texture || mat.GetTexture("_EmissionMap")!=texture || !mat.IsKeywordEnabled("_ALPHATEST_ON"))throw new Exception("Incomplete symbol material");
        var importer=(TextureImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture));
        if(!importer.DoesSourceTextureHaveAlpha() || !importer.alphaIsTransparency || !importer.mipmapEnabled || importer.filterMode!=FilterMode.Trilinear)throw new Exception("Symbol alpha/import configuration invalid");
        EditorSceneManager.OpenScene("Assets/Scenes/SampleScene.unity");
        var room=Object.FindObjectsByType<Transform>().First(t=>t.name=="Basement"&&!t.parent);
        var boss=room.Find("BossRoom");
        int uses=boss.GetComponentsInChildren<MeshRenderer>(true).Sum(r=>r.sharedMaterials.Count(m=>m==mat));
        if(uses!=1)throw new Exception("Expected one symbol renderer in map, got "+uses);
        float floor=BasementFinalBuilder.Floor(boss,"Main_Floor"),front=boss.Find("Walls/Entrance_Wall_Header").localPosition.z;
        ShaderUtil.allowAsyncCompilation=false;
        var render=typeof(BasementSurfaceBuilder).GetMethod("Render",BindingFlags.NonPublic|BindingFlags.Static);
        render.Invoke(null,new object[]{room,boss,V(-3.5f,floor+1.84f,front+21),V(-12,floor+6.5f,front+32),"Altar-NewSymbol",false});
        render.Invoke(null,new object[]{room,boss,V(0,floor+1.84f,front+2),V(-10,floor+4.8f,front+32),"Altar-NewSymbol-Wide",false});
        foreach(var message in ShaderUtil.GetShaderMessages(mat.shader))if(message.severity==UnityEditor.Rendering.ShaderCompilerMessageSeverity.Error)throw new Exception(message.message);
        Debug.Log("BASEMENT_SYMBOL_VERIFIED: alpha, PBR/emission bindings, one map renderer and two actual URP captures.");
    }
}
