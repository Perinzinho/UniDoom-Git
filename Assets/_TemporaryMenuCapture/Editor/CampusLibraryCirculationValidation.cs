using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

public static partial class CampusLibraryCirculation
{
    sealed class Surface{public Bounds bounds;public Vector3[] vertices;public int[] indices;}
    static bool RayTriangle(Ray ray,Vector3 a,Vector3 b,Vector3 c,float max)
    {
        var ab=b-a;var ac=c-a;var cross=Vector3.Cross(ray.direction,ac);float det=Vector3.Dot(ab,cross);if(Mathf.Abs(det)<.0000001f)return false;float inv=1/det;var from=ray.origin-a;float u=Vector3.Dot(from,cross)*inv;if(u<0||u>1)return false;var q=Vector3.Cross(from,ab);float v=Vector3.Dot(ray.direction,q)*inv;if(v<0||u+v>1)return false;float distance=Vector3.Dot(ac,q)*inv;return distance>=0&&distance<=max;
    }
    static int ProbeWalls(GameObject root,StringBuilder sb)
    {
        var surfaces=new List<Surface>();foreach(var mf in root.GetComponentsInChildren<MeshFilter>())
        {
            var renderer=mf.GetComponent<Renderer>();if(!renderer||!renderer.enabled||!mf.sharedMesh||!AssetDatabase.GetAssetPath(mf.sharedMesh).StartsWith("Assets/_Project/Scenes/CampusLibrary/Meshes/"))continue;if(!renderer.sharedMaterials.Any(m=>m&&(m.name.Contains("plaster")||m.name.Contains("glazing")||m.name.Contains("Brushed dark steel"))))continue;var mesh=mf.sharedMesh;if(mesh.triangles.Length==0)continue;surfaces.Add(new Surface{bounds=renderer.bounds,vertices=mesh.vertices.Select(mf.transform.TransformPoint).ToArray(),indices=mesh.triangles});
        }
        bool Hit(Vector3 point,Vector3 axis){var ray=new Ray(point-axis*.40f,axis);foreach(var surface in surfaces){if(!surface.bounds.IntersectRay(ray,out float d)||d>.80f)continue;var vv=surface.vertices;var tt=surface.indices;for(int i=0;i<tt.Length;i+=3)if(RayTriangle(ray,vv[tt[i]],vv[tt[i+1]],vv[tt[i+2]],.8f))return true;}return false;}
        int total=0,missing=0;void Probe(string name,Vector3 p,Vector3 axis){total++;bool ok=Hit(p,axis);if(!ok)missing++;sb.AppendLine((ok?"CLOSED ":"GAP ")+name+" "+V(p));}
        foreach(float x in new[]{-28.15f,28.15f})for(int bay=0;bay<6;bay++)foreach(float y in new[]{1.45f,2.25f,3.05f})Probe("window frame seam",new Vector3(x,y,bay==5?30.12f:15.115f+3*bay),Vector3.right);
        foreach(float x in new[]{-27.90f,-19.5125f,-19.3875f,-11.1125f})foreach(float y in new[]{.4f,1.4f,2.6f})Probe("study vertical junction",new Vector3(x,y,24),Vector3.forward);
        foreach(float x in new[]{-26f,-21f,-17.7f,-12.8f})foreach(float y in new[]{3.3f,4.0f,4.5f})Probe("study upper front",new Vector3(x,y,24),Vector3.forward);
        foreach(float x in new[]{-19.45f,-11.05f})foreach(float z in new[]{25f,28f})foreach(float y in new[]{3.6f,4.3f})Probe("study upper partition",new Vector3(x,y,z),Vector3.right);
        sb.AppendLine("Targeted visual triangle coverage="+(total-missing)+"/"+total+"; sampled actual rendered triangles, both face directions, not physics boxes.");return missing;
    }
    static void WallProbe(){var root=PrefabUtility.LoadPrefabContents(PrefabPath);try{var sb=new StringBuilder();ProbeWalls(root,sb);File.WriteAllText(Folder+"wall-probe.txt",sb.ToString());}finally{PrefabUtility.UnloadPrefabContents(root);}}
    static bool OverlapVolume(Bounds a,Bounds b)=>Mathf.Min(a.max.x,b.max.x)-Mathf.Max(a.min.x,b.min.x)>.02f&&Mathf.Min(a.max.y,b.max.y)-Mathf.Max(a.min.y,b.min.y)>.02f&&Mathf.Min(a.max.z,b.max.z)-Mathf.Max(a.min.z,b.min.z)>.02f;
    static void Validate()
    {
        var root=PrefabUtility.LoadPrefabContents(PrefabPath);var sb=new StringBuilder();try
        {
            int gaps=ProbeWalls(root,sb);if(gaps>0)throw new Exception("Targeted wall coverage still has gaps: "+gaps);
            var boxes=root.GetComponentsInChildren<BoxCollider>().Where(b=>b.enabled&&!b.isTrigger).ToArray();foreach(var sector in new[]{("6m passage approach",Area(new Vector3(-3,.08f,9),new Vector3(3,2.7f,15.2f))),("Sanitary corridor",Area(new Vector3(16.15f,.05f,2.8f),new Vector3(18.8f,2.55f,11.9f)))}){var obstacles=boxes.Where(b=>OverlapVolume(sector.Item2,WorldBox(b))).ToArray();sb.AppendLine(sector.Item1+" solid box obstacles="+obstacles.Length+" "+string.Join(",",obstacles.Select(b=>b.name)));if(obstacles.Length>0)throw new Exception("Obstructed clearance: "+sector.Item1);}
            var vestibule=Area(new Vector3(15.58f,0,2.52f),new Vector3(19.08f,4.6f,12.15f));float closest=100;foreach(var shelf in boxes.Where(b=>b.name.StartsWith("Bookcase row ")||b.name=="Double sided stack collision")){var b=WorldBox(shelf);float dx=Mathf.Max(0,Mathf.Max(vestibule.min.x-b.max.x,b.min.x-vestibule.max.x)),dz=Mathf.Max(0,Mathf.Max(vestibule.min.z-b.max.z,b.min.z-vestibule.max.z));closest=Mathf.Min(closest,Mathf.Sqrt(dx*dx+dz*dz));}sb.AppendLine("Minimum stack collision separation from sanitary vestibule="+closest);if(closest<2)throw new Exception("Stacks remain too close to sanitary vestibule");
        }catch(Exception e){sb.AppendLine(e.ToString());File.WriteAllText(Folder+"validation.txt",sb.ToString());throw;}finally{PrefabUtility.UnloadPrefabContents(root);}
        var previous=SceneManager.GetActiveScene();var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Additive);try
        {
            SceneManager.SetActiveScene(scene);var roots=scene.GetRootGameObjects();var all=roots.SelectMany(r=>r.GetComponentsInChildren<Transform>(true)).ToArray();int missing=all.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)),invalid=all.SelectMany(t=>t.GetComponents<Renderer>()).Count(r=>r.sharedMaterials.Any(m=>!m||!m.shader||m.shader.name.Contains("Error")));sb.AppendLine("Missing scripts="+missing+"; invalid materials="+invalid);if(missing+invalid>0)throw new Exception("Invalid scene");
            var chairs=all.Where(t=>t.name=="Campus reading chair"||t.name=="North wing reading chair").ToArray();int shelves=all.Count(t=>t.name=="Campus full bookcase"||t.name=="North wing full bookcase");if(chairs.Length!=95||shelves!=62)throw new Exception("Furniture count changed");Physics.SyncTransforms();int supported=0;foreach(var chair in chairs){var b=RenderBounds(chair.gameObject);if(Physics.Raycast(new Vector3(chair.position.x,b.min.y+.06f,chair.position.z),Vector3.down,out var hit,.25f,~0,QueryTriggerInteraction.Ignore)&&Mathf.Abs(hit.point.y-b.min.y)<.02f)supported++;}sb.AppendLine("Chairs="+chairs.Length+"; shelves="+shelves+"; floor-supported chairs="+supported);if(supported!=95)throw new Exception("Chair floor support failed");
            var sources=new List<NavMeshBuildSource>();foreach(var r in roots)NavMeshBuilder.CollectSources(r.transform,~0,NavMeshCollectGeometry.PhysicsColliders,0,new List<NavMeshBuildMarkup>(),sources);var settings=NavMesh.GetSettingsByID(0);settings.agentRadius=.27f;settings.agentHeight=2.141f;settings.agentClimb=.36f;settings.agentSlope=45;settings.overrideVoxelSize=true;settings.voxelSize=.08f;var nav=NavMeshBuilder.BuildNavMeshData(settings,sources,new Bounds(new Vector3(0,2,9),new Vector3(64,12,52)),Vector3.zero,Quaternion.identity);var handle=NavMesh.AddNavMeshData(nav);
            try
            {
                if(!NavMesh.SamplePosition(new Vector3(0,.64f,-9.6f),out var start,1,NavMesh.AllAreas))throw new Exception("Arrival navigation missing");var targets=new[]{new Vector3(-15,0,0),new Vector3(-15,0,9),new Vector3(5.5f,0,0),new Vector3(17.6f,0,1.6f),new Vector3(17.6f,0,6.4f),new Vector3(0,0,11),new Vector3(0,0,14),new Vector3(0,0,28),new Vector3(-20,0,14.3f),new Vector3(-15.1f,0,19),new Vector3(-7.2f,0,23),new Vector3(-23.65f,0,25),new Vector3(-15.25f,0,25),new Vector3(-21,0,29),new Vector3(-14,0,29),new Vector3(-8.6f,0,27.8f),new Vector3(8.6f,0,27.8f),new Vector3(7.75f,0,17),new Vector3(12.25f,0,22),new Vector3(25.7f,0,28.5f),new Vector3(21,0,5.9f),new Vector3(23.15f,0,5.9f),new Vector3(25.3f,0,5.9f),new Vector3(21,0,10.6f),new Vector3(23.15f,0,10.6f),new Vector3(25.3f,0,10.6f),new Vector3(25.1f,0,18),new Vector3(25.1f,0,22),new Vector3(-25.7f,0,3.8f),new Vector3(17.6f,0,8),new Vector3(17.6f,0,11.4f),new Vector3(22.9f,0,.9f),new Vector3(-2.4f,0,11),new Vector3(2.4f,0,11)};int passed=0;foreach(var target in targets){var p=new NavMeshPath();bool ok=NavMesh.SamplePosition(target,out var end,.45f,NavMesh.AllAreas)&&NavMesh.CalculatePath(start.position,end.position,NavMesh.AllAreas,p)&&p.status==NavMeshPathStatus.PathComplete;sb.AppendLine((ok?"PASS ":"FAIL ")+V(target));if(ok)passed++;}sb.AppendLine("Navigation routes="+passed+"/"+targets.Length+"; agent h2.141 r.27 voxel.08; temporary navmesh not saved.");if(passed!=targets.Length)throw new Exception("Navigation route failed");
            }finally{handle.Remove();Object.DestroyImmediate(nav);}
        }catch(Exception e){sb.AppendLine(e.ToString());throw;}finally{File.WriteAllText(Folder+"validation.txt",sb.ToString());SceneManager.SetActiveScene(previous);EditorSceneManager.CloseScene(scene,true);}
    }
}
