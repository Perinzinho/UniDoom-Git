using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

/// <summary>Renders only a nearby, visible washroom mirror, at a capped update rate.</summary>
[ExecuteAlways, DisallowMultipleComponent, RequireComponent(typeof(MeshRenderer))]
public sealed class LawSchoolMirror : MonoBehaviour
{
    [SerializeField] int resolution=512;
    [SerializeField] float maximumDistance=16f;
    Camera reflectionCamera;
    RenderTexture reflectionTexture;
    MaterialPropertyBlock properties;
    MeshRenderer surface;
    double nextFrame;
    Camera lastSource;
    readonly Plane[] frustum=new Plane[6];
    readonly List<Renderer> playerRenderers=new List<Renderer>(8);
    readonly List<bool> playerVisibility=new List<bool>(8);
    static bool rendering;
    void OnEnable(){surface=GetComponent<MeshRenderer>();properties=new MaterialPropertyBlock();RenderPipelineManager.beginCameraRendering+=Render;}
    void OnDisable(){RenderPipelineManager.beginCameraRendering-=Render;Release();}
    void Release(){if(reflectionCamera){if(Application.isPlaying)Destroy(reflectionCamera.gameObject);else DestroyImmediate(reflectionCamera.gameObject);}if(reflectionTexture){reflectionTexture.Release();if(Application.isPlaying)Destroy(reflectionTexture);else DestroyImmediate(reflectionTexture);}reflectionCamera=null;reflectionTexture=null;}
    void Render(ScriptableRenderContext context,Camera source)
    {
        if(rendering||!surface||!surface.enabled||source.cameraType!=CameraType.Game&&source.cameraType!=CameraType.SceneView||source==reflectionCamera||Application.isPlaying&&source.cameraType==CameraType.SceneView)return;
        Vector3 normal=-transform.forward;float distance=Vector3.Distance(source.transform.position,transform.position);
        if(distance>maximumDistance||Vector3.Dot(source.transform.position-transform.position,normal)<=.03f)return;
        GeometryUtility.CalculateFrustumPlanes(source,frustum);
        if(!GeometryUtility.TestPlanesAABB(frustum,surface.bounds))return;
        if(lastSource==source&&Time.realtimeSinceStartupAsDouble<nextFrame&&source.cameraType==CameraType.Game)return;
        lastSource=source;
        nextFrame=Time.realtimeSinceStartupAsDouble+1.0/15;
        if(!reflectionCamera){var go=new GameObject("LawSchool mirror camera"){hideFlags=HideFlags.HideAndDontSave};reflectionCamera=go.AddComponent<Camera>();reflectionCamera.enabled=false;reflectionTexture=new RenderTexture(resolution,resolution,24,RenderTextureFormat.ARGB32){name="LawSchool mirror",hideFlags=HideFlags.HideAndDontSave};reflectionTexture.Create();}
        reflectionCamera.CopyFrom(source);reflectionCamera.enabled=false;reflectionCamera.targetTexture=reflectionTexture;reflectionCamera.cameraType=CameraType.Reflection;reflectionCamera.cullingMask=source.cullingMask&~(1<<5);reflectionCamera.useOcclusionCulling=false;reflectionCamera.farClipPlane=Mathf.Min(source.farClipPlane,40);
        var additional=reflectionCamera.GetUniversalAdditionalCameraData();additional.renderPostProcessing=false;additional.renderShadows=false;additional.requiresColorOption=CameraOverrideOption.Off;additional.requiresDepthOption=CameraOverrideOption.Off;
        float d=-Vector3.Dot(normal,transform.position)-.02f;Vector4 plane=new Vector4(normal.x,normal.y,normal.z,d);Matrix4x4 reflect=Matrix4x4.identity;
        for(int row=0;row<3;row++)for(int col=0;col<4;col++)reflect[row,col]-=2*plane[row]*plane[col];
        reflectionCamera.worldToCameraMatrix=source.worldToCameraMatrix*reflect;
        reflectionCamera.transform.position=reflect.MultiplyPoint(source.transform.position);reflectionCamera.transform.rotation=Quaternion.LookRotation(reflect.MultiplyVector(source.transform.forward),reflect.MultiplyVector(source.transform.up));
        var point=reflectionCamera.worldToCameraMatrix.MultiplyPoint(transform.position+normal*.025f);var n=reflectionCamera.worldToCameraMatrix.MultiplyVector(normal).normalized;reflectionCamera.projectionMatrix=source.CalculateObliqueMatrix(new Vector4(n.x,n.y,n.z,-Vector3.Dot(point,n)));
        bool old=GL.invertCulling;bool enabled=surface.enabled;
        try{rendering=true;surface.enabled=false;GL.invertCulling=!old;
            playerRenderers.Clear();playerVisibility.Clear();if(Application.isPlaying&&GameManager.SessionPlayer){GameManager.SessionPlayer.GetComponentsInChildren(false,playerRenderers);foreach(var renderer in playerRenderers){playerVisibility.Add(renderer.forceRenderingOff);renderer.forceRenderingOff=true;}}
#pragma warning disable CS0618
            UniversalRenderPipeline.RenderSingleCamera(context,reflectionCamera);
#pragma warning restore CS0618
            surface.GetPropertyBlock(properties);properties.SetTexture("_ReflectionTex",reflectionTexture);properties.SetMatrix("_ReflectionVP",GL.GetGPUProjectionMatrix(reflectionCamera.projectionMatrix,true)*reflectionCamera.worldToCameraMatrix);surface.SetPropertyBlock(properties);
        }finally{for(int i=0;i<playerRenderers.Count;i++)if(playerRenderers[i])playerRenderers[i].forceRenderingOff=playerVisibility[i];surface.enabled=enabled;GL.invertCulling=old;rendering=false;}
    }
}
