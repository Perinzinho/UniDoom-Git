using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object=UnityEngine.Object;

public static class EntranceAudit
{
    public static readonly string[] Rooms={"EntranceGarden","EntranceHallway","DirectorRoom"};
    public static string Prefab(string room)=>"Assets/_Project/Scenes/"+room+"/Prefabs/"+room+".prefab";
    public static void Run()
    {
        Directory.CreateDirectory("Evidence");
        var text=new StringBuilder();
        foreach(var room in Rooms)
        {
            var root=PrefabUtility.LoadPrefabContents(Prefab(room));
            text.AppendLine("ROOM "+room+" scale="+root.transform.localScale);
            foreach(var t in root.GetComponentsInChildren<Transform>(true))
            {
                var depth=0;for(var p=t.parent;p;p=p.parent)depth++;
                if(depth>3)continue;
                var r=t.GetComponent<Renderer>();var b=r?r.bounds:new Bounds();
                text.AppendLine(new string(' ',depth*2)+t.name+" p="+t.localPosition+" s="+t.localScale+" world="+t.position+" bounds="+b+" mats="+(r?string.Join(",",r.sharedMaterials.Select(m=>m?m.name:"NULL")):"")+" scripts="+string.Join(",",t.GetComponents<MonoBehaviour>().Where(m=>m).Select(m=>m.GetType().Name)));
            }
            PrefabUtility.UnloadPrefabContents(root);
        }
        EditorSceneManager.OpenScene("Assets/_Project/Scenes/Main/SampleScene.unity");
        foreach(var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
        {
            if(!Rooms.Contains(t.name))continue;
            text.AppendLine("MAP "+t.name+" world="+t.position+" scale="+t.lossyScale+" rot="+t.eulerAngles);
            foreach(Transform c in t){var r=c.GetComponent<Renderer>();text.AppendLine("  "+c.name+" p="+c.localPosition+" s="+c.localScale+" bounds="+(r?r.bounds.ToString():""));}
        }
        File.WriteAllText("Evidence/baseline.txt",text.ToString());
        Debug.Log("ENTRANCE_AUDIT_PASS");
    }
}
