using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TotalChaosMaps;

public static class LibraryLayoutAudit
{
    static string PathOf(Transform t)=>t.parent?PathOf(t.parent)+"/"+t.name:t.name;
    public static void Run()
    {
        var sb=new StringBuilder();EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        var room=GameObject.Find("Library");var art=room.transform.Find("Abandoned Library Interior");
        var entry=art.TransformPoint(new Vector3(10.1f,1.3f,19.7f));sb.AppendLine("ENTRY WORLD "+entry);
        foreach(var c in UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None).Where(c=>!c.isTrigger).OrderBy(c=>Vector3.Distance(c.ClosestPoint(entry),entry)).Take(16))
            sb.AppendLine("NEAR COLLIDER "+PathOf(c.transform)+" bounds="+c.bounds+" relative="+art.InverseTransformPoint(c.bounds.center)+" scripts="+string.Join(",",c.GetComponentsInParent<MonoBehaviour>().Where(b=>b).Select(b=>b.GetType().Name)));
        foreach(var region in UnityEngine.Object.FindObjectsByType<TCRegionAtmosphere>(FindObjectsSortMode.None))
            if(PathOf(region.transform).ToLowerInvariant().Contains("class"))sb.AppendLine("CLASS REGION "+PathOf(region.transform)+" ambient="+region.ambient+" fog="+region.fogDensity);
        foreach(var light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
            if(PathOf(light.transform).ToLowerInvariant().Contains("class"))sb.AppendLine("CLASS LIGHT "+PathOf(light.transform)+" intensity="+light.intensity+" range="+light.range+" color="+light.color);
        foreach(var door in UnityEngine.Object.FindObjectsByType<OpenDoorWithKey>(FindObjectsSortMode.None).OrderBy(d=>Vector3.Distance(d.transform.position,entry)).Take(3))
        {
            sb.AppendLine("DOOR "+PathOf(door.transform)+" world="+door.transform.position+" relative="+art.InverseTransformPoint(door.transform.position));
            var so=new SerializedObject(door);sb.AppendLine("Keys "+so.FindProperty("requiresKey01").boolValue+","+so.FindProperty("requiresKey02").boolValue+","+so.FindProperty("requiresKey03").boolValue);
            foreach(var t in door.GetComponentsInChildren<Transform>(true))
            {
                var r=t.GetComponent<Renderer>();var c=t.GetComponent<BoxCollider>();
                sb.AppendLine("  "+PathOf(t)+" local="+t.localPosition+" scale="+t.localScale+(r?" bounds="+r.bounds+" mats="+string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")):"")+(c?" BOX "+c.size+" trigger="+c.isTrigger:"") );
            }
        }
        Directory.CreateDirectory("Evidence/Layout");File.WriteAllText("Evidence/Layout/audit.txt",sb.ToString());Debug.Log("LIBRARY_LAYOUT_AUDIT_PASS");
    }
}
