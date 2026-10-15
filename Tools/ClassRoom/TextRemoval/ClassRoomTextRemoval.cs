using System;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// One-off Editor utility: copy to Assets/Editor in an isolated .utmp project.
public static class ClassRoomTextRemoval
{
    const string Prefab = "Assets/prefabs/Scenario/ClassRoom.prefab";
    const string Interior = "Assets/Art/ClassRoom/AbandonedClassRoomInterior.prefab";
    const string Output = ".utmp/ClassRoomTextRemoval/";

    public static void Run()
    {
        if (!Application.dataPath.Replace('\\', '/').Contains("/.utmp/"))
            throw new Exception("Run in an isolated project");
        Directory.CreateDirectory(Output);
        if (!File.Exists(Output + "Interior.before.prefab")) File.Copy(Interior, Output + "Interior.before.prefab");
        var root = PrefabUtility.LoadPrefabContents(Interior);
        int count = 0;
        try
        {
            foreach (var text in root.GetComponentsInChildren<TextMesh>(true))
            {
                if (string.IsNullOrEmpty(text.text)) continue;
                text.text = string.Empty;
                var renderer = text.GetComponent<Renderer>();
                if (renderer) renderer.enabled = false;
                count++;
            }
            foreach (var text in root.GetComponentsInChildren<TMPro.TMP_Text>(true))
            { if (string.IsNullOrEmpty(text.text)) continue; text.text = string.Empty; text.enabled = false; count++; }
            foreach (var text in root.GetComponentsInChildren<UnityEngine.UI.Text>(true))
            { if (string.IsNullOrEmpty(text.text)) continue; text.text = string.Empty; text.enabled = false; count++; }
            if (count > 0)
            {
                PrefabUtility.SaveAsPrefabAsset(root, Interior, out bool saved);
                if (!saved) throw new Exception("Could not save ClassRoom interior");
            }
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Verify();
        typeof(ClassRoomPerformanceAudit).GetMethod("Capture", BindingFlags.Static | BindingFlags.NonPublic)
            .Invoke(null, new object[] { "without-text" });
        File.WriteAllText(Output + "validation.txt", "Visible texts cleared: " + count +
            ". Full ClassRoom hierarchy, including inactive objects: no nonempty TextMesh/TMP/UI text.\n" +
            "Rendered eight views across PC and Mobile pipelines on the desktop GPU.\n" +
            "No new Player build or gameplay test; previous optimization build predates text removal.\n");
        Debug.Log("CLASSROOM_TEXT_REMOVAL_OK count=" + count);
    }

    public static void Verify()
    {
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            if (root.GetComponentsInChildren<TextMesh>(true).Any(t => !string.IsNullOrEmpty(t.text)) ||
                root.GetComponentsInChildren<TMPro.TMP_Text>(true).Any(t => !string.IsNullOrEmpty(t.text)) ||
                root.GetComponentsInChildren<UnityEngine.UI.Text>(true).Any(t => !string.IsNullOrEmpty(t.text)))
                throw new Exception("ClassRoom still contains text");
            if (root.GetComponentsInChildren<TextMesh>(true).Any(t => t.GetComponent<Renderer>()?.enabled == true))
                throw new Exception("A text renderer is still enabled");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        Debug.Log("CLASSROOM_NO_TEXT_VERIFIED");
    }
}
