using System;
using System.IO;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniDoom.UI;
using Object=UnityEngine.Object;

[InitializeOnLoad]
public static class CinemaCreditsPreview
{
    const string Work=".local/CinemaCredits/";
    static bool busy;
    static CinemaCreditsPreview(){Directory.CreateDirectory(Work);File.WriteAllText(Work+"ready.txt","cinema-v1");EditorApplication.update+=Poll;}
    static void Poll()
    {
        if(busy||EditorApplication.isCompiling||EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||!File.Exists(Work+"request.txt"))return;
        busy=true;File.Delete(Work+"request.txt");
        try{Run();File.WriteAllText(Work+"done.txt",DateTime.UtcNow.ToString("o"));}catch(Exception e){File.WriteAllText(Work+"error.txt",e.ToString());Debug.LogException(e);}finally{busy=false;}
    }
    static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
    static void Set(object o,string name,object value)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);
    static object Get(object o,string name)=>o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);
    static void Call(object o,string name,params object[] args)=>o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,args);
    static void Run()
    {
        var state=GameMenus.Instance;float timeScale=Time.timeScale;bool audioPause=AudioListener.pause;var active=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var report=new System.Text.StringBuilder();
        Check(CreditsSettings.RollOffset(0,4000,1080,24,2)==-1080,"Roll must begin below screen");
        Check(CreditsSettings.RollOffset(2,4000,1080,24,2)==-1080,"Start delay missing");
        foreach(int fps in new[]{30,60,144})
        {float previous=-1080;for(int step=0;step<=fps*12;step++){float at=CreditsSettings.RollOffset((float)step/fps,4000,1080,24,2);Check(at>=previous,"Roll reverses");previous=at;}Check(Mathf.Abs(previous-(-840))<.001f,"Speed differs with frame rate");}
        Check(CreditsSettings.RollOffset(9999,4000,1080,24,2)==4000,"End must clear the complete roll above the screen");
        report.AppendLine("PASS absolute unscaled-time roll at 30/60/144 fps, start delay, monotonic movement and complete offscreen ending; no input or runtime Update invoked.");
        var config=Resources.Load<CreditsSettings>("Menus/Credits");Check(config&&config.Content,"Credits settings/content missing");
        foreach(var resolution in new[]{new Vector2Int(1920,1080),new Vector2Int(800,600),new Vector2Int(3440,1440)})
        {
            var scene=EditorSceneManager.NewPreviewScene();var root=new GameObject("Offscreen cinematic credits",typeof(RectTransform),typeof(Canvas)){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root,scene);
            float scale=Mathf.Sqrt((float)resolution.x/1920*resolution.y/1080);Vector2 size=new Vector2(resolution.x/scale,resolution.y/scale);var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.WorldSpace;((RectTransform)root.transform).sizeDelta=size;
            var cameraGO=new GameObject("Offscreen camera"){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraGO,scene);var camera=cameraGO.AddComponent<Camera>();camera.overrideSceneCullingMask=EditorSceneManager.GetSceneCullingMask(scene);camera.orthographic=true;camera.orthographicSize=size.y*.5f;camera.transform.position=new Vector3(0,0,-10);camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.magenta;camera.nearClipPlane=.1f;camera.farClipPlane=30;canvas.worldCamera=camera;
            var target=new RenderTexture(resolution.x,resolution.y,24);camera.targetTexture=target;var pixels=new Texture2D(resolution.x,resolution.y,TextureFormat.RGB24,false);
            var host=new GameObject("Inactive credits builder"){hideFlags=HideFlags.HideAndDontSave};UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host,scene);host.SetActive(false);var menu=host.AddComponent<GameMenus>();menu.enabled=false;
            var page=new GameObject("Credits",typeof(RectTransform));page.transform.SetParent(root.transform,false);Set(menu,"canvas",canvas);Set(menu,"credits",page);
            try
            {
                Canvas.ForceUpdateCanvases();Call(menu,"BuildCredits");Canvas.ForceUpdateCanvases();Call(menu,"FitCreditsRoll",true);
                var text=(TMP_Text)Get(menu,"creditsRollText");var viewport=(RectTransform)Get(menu,"creditsViewport");float height=text.rectTransform.rect.height;float duration=config.StartDelay+(height+viewport.rect.height)/config.ScrollSpeed+config.EndHold;
                Check(page.transform.parent==canvas.transform&&Vector2.Distance(((RectTransform)page.transform).rect.size,size)<.1f,"Credits do not fill canvas");
                Check(page.GetComponentsInChildren<ScrollRect>().Length==0&&page.GetComponentsInChildren<Scrollbar>().Length==0&&page.GetComponentsInChildren<Button>().Length==0,"Reader controls remain");
                var background=page.GetComponentInChildren<Image>();Check(background.color==Color.black&&Vector2.Distance(background.rectTransform.rect.size,size)<.1f,"Background is not full black");
                Check(text.color==Color.white&&text.alignment==TextAlignmentOptions.Top&&Mathf.Abs(text.rectTransform.anchoredPosition.x)<.01f,"Credits not white/centred");
                Check(text.text.Contains("Leonardo Perin")&&!text.text.Contains("http")&&!text.text.Contains("•"),"Credits content changed or links/bullets visible");
                void Shot(float elapsed,string label)
                {
                    Call(menu,"PositionCreditsRoll",elapsed);Canvas.ForceUpdateCanvases();camera.Render();camera.Render();var old=RenderTexture.active;RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,target.width,target.height),0,0);pixels.Apply();RenderTexture.active=old;
                    Check(pixels.GetPixel(0,0).maxColorComponent<.01f&&pixels.GetPixel(target.width-1,target.height-1).maxColorComponent<.01f,"Uncovered screen corner");
                    File.WriteAllBytes("output/Creditos-Cinema-"+label+"-"+resolution.x+"x"+resolution.y+".png",pixels.EncodeToPNG());
                }
                Shot(27,"Inicio");Shot(config.StartDelay+(height*.5f+viewport.rect.height*.5f)/config.ScrollSpeed,"Meio");Shot(config.StartDelay+(height+viewport.rect.height*.5f)/config.ScrollSpeed,"Final");
                if(resolution.x==1920)
                {
                    Directory.CreateDirectory(Work+"frames");var videoTarget=new RenderTexture(1280,720,24);var videoPixels=new Texture2D(1280,720,TextureFormat.RGB24,false);camera.targetTexture=videoTarget;
                    try{for(int frame=0;frame<90;frame++){Call(menu,"PositionCreditsRoll",20f+frame/15f);Canvas.ForceUpdateCanvases();camera.Render();var old=RenderTexture.active;RenderTexture.active=videoTarget;videoPixels.ReadPixels(new Rect(0,0,1280,720),0,0);videoPixels.Apply();RenderTexture.active=old;File.WriteAllBytes(Work+"frames/frame"+frame.ToString("000")+".png",videoPixels.EncodeToPNG());}}
                    finally{camera.targetTexture=target;Object.DestroyImmediate(videoPixels);Object.DestroyImmediate(videoTarget);}
                }
                report.AppendLine("PASS "+resolution+": black fullscreen, white centred text, no reader controls, autonomous positions; content="+height+" viewport="+viewport.rect.height+" duration estimate="+duration+"s.");
            }
            finally{camera.targetTexture=null;Object.DestroyImmediate(pixels);Object.DestroyImmediate(target);Object.DestroyImmediate(host);Object.DestroyImmediate(root);Object.DestroyImmediate(cameraGO);EditorSceneManager.ClosePreviewScene(scene);}
        }
        Check(GameMenus.Instance==state&&Time.timeScale==timeScale&&AudioListener.pause==audioPause&&UnityEngine.SceneManagement.SceneManager.GetActiveScene()==active&&!EditorApplication.isPlayingOrWillChangePlaymode,"Preview changed active game/editor state");
        report.AppendLine("No game, Play Mode, window activation/focus, audio playback or runtime menu transition. Preview uses isolated inactive builder and production layout/position methods.");
        File.WriteAllText(Work+"validation.txt",report.ToString());
    }
}
