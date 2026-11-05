using System;
using UnityEngine;

/// <summary>Keeps persistent entrance dressing at metric scale when scene instances resize the shell.</summary>
[ExecuteAlways, DisallowMultipleComponent]
public sealed class EntranceDressingLayout : MonoBehaviour
{
    [Serializable] public struct Placement
    {
        public Transform target;
        public Vector3 position;
        public Vector3 scale;
        public bool stretchX, stretchZ;
        public int sideWall;
    }
    public MeshFilter floor;
    public Vector2 referenceSize;
    public bool clipToSideWalls;
    public Vector2 Size { get; private set; }
    public Placement[] placements=Array.Empty<Placement>();
    Matrix4x4 previousFloor,previousParent;
    Bounds previousBounds;
    bool initialized;
    void OnEnable()=>Refresh();
    void OnValidate(){initialized=false;Refresh();}
    void LateUpdate()=>Refresh();
    public void Refresh()
    {
        if(!floor||!floor.sharedMesh||!transform.parent||referenceSize.x<=0||referenceSize.y<=0)return;
        var parent=transform.parent;var bounds=floor.sharedMesh.bounds;
        var fm=floor.transform.localToWorldMatrix;var pm=parent.localToWorldMatrix;
        if(initialized&&fm==previousFloor&&pm==previousParent&&bounds==previousBounds)return;
        previousFloor=fm;previousParent=pm;previousBounds=bounds;initialized=true;
        var local=new Bounds(parent.InverseTransformPoint(floor.transform.TransformPoint(bounds.center)),Vector3.zero);
        for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
            local.Encapsulate(parent.InverseTransformPoint(floor.transform.TransformPoint(bounds.center+Vector3.Scale(bounds.extents,new Vector3(x,y,z)))));
        if(clipToSideWalls)
        {
            var min=local.min;var max=local.max;
            foreach(Transform wall in parent)
            {
                if(!wall.name.StartsWith("Wall",StringComparison.Ordinal))continue;
                var filter=wall.GetComponent<MeshFilter>();var collider=wall.GetComponent<BoxCollider>();
                var wb=filter&&filter.sharedMesh?filter.sharedMesh.bounds:collider?new Bounds(collider.center,collider.size):new Bounds(Vector3.zero,Vector3.one);
                var b=new Bounds(parent.InverseTransformPoint(wall.TransformPoint(wb.center)),Vector3.zero);
                for(int x=-1;x<=1;x+=2)for(int y=-1;y<=1;y+=2)for(int z=-1;z<=1;z+=2)
                    b.Encapsulate(parent.InverseTransformPoint(wall.TransformPoint(wb.center+Vector3.Scale(wb.extents,new Vector3(x,y,z)))));
                if(wall.localPosition.x<0)min.x=Mathf.Max(min.x,b.max.x);else max.x=Mathf.Min(max.x,b.min.x);
            }
            local.SetMinMax(min,max);
        }
        var s=parent.lossyScale;s=new Vector3(Mathf.Max(.001f,Mathf.Abs(s.x)),Mathf.Max(.001f,Mathf.Abs(s.y)),Mathf.Max(.001f,Mathf.Abs(s.z)));
        transform.localPosition=new Vector3(local.center.x,local.max.y,local.center.z);transform.localRotation=Quaternion.identity;transform.localScale=new Vector3(1/s.x,1/s.y,1/s.z);
        var ratio=new Vector2(local.size.x*s.x/referenceSize.x,local.size.z*s.z/referenceSize.y);
        Size=new Vector2(local.size.x*s.x,local.size.z*s.z);
        foreach(var p in placements)
        {
            if(!p.target)continue;
            if(p.sideWall!=0)
            {
                Transform wall=null;
                foreach(Transform child in parent)if(child.name.StartsWith("Wall",StringComparison.Ordinal)&&Mathf.Sign(child.localPosition.x)==p.sideWall)wall=child;
                p.target.gameObject.SetActive(wall);
                if(!wall)continue;
                var filter=wall.GetComponent<MeshFilter>();var bc=wall.GetComponent<BoxCollider>();
                var wb=filter&&filter.sharedMesh?filter.sharedMesh.bounds:bc?new Bounds(bc.center,bc.size):new Bounds(Vector3.zero,Vector3.one);
                float top=parent.InverseTransformPoint(wall.TransformPoint(wb.max)).y;
                float h=Mathf.Max(.1f,(top-local.max.y)*s.y);
                p.target.localPosition=new Vector3(p.sideWall*(Size.x*.5f-.025f),h*.5f,0);
                p.target.localScale=new Vector3(.035f,h,Size.y);
                continue;
            }
            p.target.localPosition=new Vector3(p.position.x*ratio.x,p.position.y,p.position.z*ratio.y);
            p.target.localScale=Vector3.Scale(p.scale,new Vector3(p.stretchX?ratio.x:1,1,p.stretchZ?ratio.y:1));
        }
    }
}
