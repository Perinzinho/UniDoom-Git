using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.ProBuilder;
using Object = UnityEngine.Object;

public static class CampusAudit
{
    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/.utmp/EntranceRebuild/")) throw new Exception("Isolated workspace required");
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        var text = new StringBuilder();
        foreach (var root in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.parent == null))
        {
            var renderers = root.GetComponentsInChildren<Renderer>().Where(r=>r.enabled).ToArray();
            if (renderers.Length == 0) continue;
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            text.AppendLine(root.name + " position=" + root.position + " rotation=" + root.eulerAngles + " scale=" + root.lossyScale + " bounds=" + bounds);
        }
        foreach (var name in new[] { "EntranceGarden", "EntranceHallway" })
        {
            var root = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).First(t=>t.name==name);
            foreach (Transform child in root)
            {
                var renderer = child.GetComponent<Renderer>();
                text.AppendLine(name+"/"+child.name+" local="+child.localPosition+" scale="+child.localScale+" bounds="+(renderer?renderer.bounds.ToString():""));
            }
        }
        foreach(var cc in Object.FindObjectsByType<CharacterController>(FindObjectsSortMode.None)) text.AppendLine("PLAYER " + cc.name+" position="+cc.transform.position+" height="+cc.height+" radius="+cc.radius);
        Directory.CreateDirectory("CampusEvidence");
        File.WriteAllText("CampusEvidence/audit.txt",text.ToString());
        Debug.Log("CAMPUS_AUDIT_PASS");
    }
}
