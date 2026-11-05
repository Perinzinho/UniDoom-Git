using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[DisallowMultipleComponent]
public sealed class LibraryEncounterDirector : MonoBehaviour
{
    [Serializable]
    public sealed class Wave
    {
        public string name;
        public Bounds activationBounds;
        public bool afterKeyPickup;
        public Transform[] points;
    }
    public GameObject zombiePrefab;
    public GameObject objectiveKey;
    public Wave[] waves;
    [Min(.1f)] public float spawnInterval=1.4f;
    [Header("Key02 power failure")]
    public Light[] blackoutLights;
    public Renderer[] blackoutTubes;
    [Min(.1f)] public float blackoutDuration=8;
    readonly HashSet<int> activated=new HashSet<int>();
    readonly List<GameObject> spawned=new List<GameObject>();
    Transform player;
    int pendingSpawns;
    public int SpawnedCount { get; private set; }
    public int ActivatedWaves=>activated.Count;
    public int PendingSpawns=>pendingSpawns;
    public bool ObjectiveCollected { get; private set; }
    public bool BlackoutActive { get; private set; }
    bool[] lightStates;
    MaterialPropertyBlock[] tubeStates;
    MaterialPropertyBlock darkTube;
    float blackoutEndsAt;

    void Start()
    {
        var found=GameObject.FindGameObjectWithTag("Player");
        if(found)BindPlayer(found.GetComponent<PlayerPickupKey>());
    }

    void OnTriggerEnter(Collider other)=>Track(other);
    void OnTriggerStay(Collider other)=>Track(other);
    void Track(Collider other)
    {
        var pickup=other.GetComponentInParent<PlayerPickupKey>();
        if(pickup&&pickup.CompareTag("Player"))BindPlayer(pickup);
    }
    void BindPlayer(PlayerPickupKey pickup)
    {
        if(!pickup||player)return;
        player=pickup.transform;
        // A return visit with Key02 must not replay the collection blackout.
        ObjectiveCollected=pickup.key02;
    }
    void Update()
    {
        if(!player||waves==null)return;
        if(!objectiveKey&&!ObjectiveCollected)
        {
            ObjectiveCollected=true;
            BeginBlackout();
        }
        var local=transform.InverseTransformPoint(player.position);
        for(int i=0;i<waves.Length;i++)
        {
            if(activated.Contains(i))continue;
            var wave=waves[i];
            if(wave.afterKeyPickup?!ObjectiveCollected:!wave.activationBounds.Contains(local))continue;
            activated.Add(i);StartCoroutine(SpawnWave(wave));
        }
    }
    IEnumerator SpawnWave(Wave wave)
    {
        if(!zombiePrefab||wave.points==null)yield break;
        pendingSpawns+=wave.points.Length;
        foreach(var point in wave.points)
        {
            if(!point){pendingSpawns--;continue;}
            // Avoid materialising a zombie inside the player if they reached a marker first.
            float waited=0;
            while(player&&Vector3.Distance(player.position,point.position)<3&&waited<5){waited+=.25f;yield return new WaitForSeconds(.25f);}
            if(player&&Vector3.Distance(player.position,point.position)<3){pendingSpawns--;continue;}
            if(NavMesh.SamplePosition(point.position,out var hit,1.5f,NavMesh.AllAreas))
            {
                var enemy=Instantiate(zombiePrefab,hit.position,point.rotation,transform);
                spawned.Add(enemy);SpawnedCount++;
            }
            pendingSpawns--;yield return new WaitForSeconds(spawnInterval);
        }
    }
    void BeginBlackout()
    {
        if(blackoutLights==null||blackoutLights.Length==0)return;
        lightStates=new bool[blackoutLights.Length];
        for(int i=0;i<lightStates.Length;i++)if(blackoutLights[i])lightStates[i]=blackoutLights[i].enabled;
        tubeStates=new MaterialPropertyBlock[blackoutTubes==null?0:blackoutTubes.Length];
        for(int i=0;i<tubeStates.Length;i++)
        {
            tubeStates[i]=new MaterialPropertyBlock();
            if(blackoutTubes[i])blackoutTubes[i].GetPropertyBlock(tubeStates[i]);
        }
        darkTube=new MaterialPropertyBlock();
        darkTube.SetColor("_EmissionColor",Color.black);
        darkTube.SetColor("_BaseColor",new Color(.055f,.06f,.065f));
        blackoutEndsAt=Time.time+blackoutDuration;
        BlackoutActive=true;
    }
    void LateUpdate()
    {
        if(!BlackoutActive)return;
        if(Time.time>=blackoutEndsAt){RestoreCircuit();return;}
        // Run after the existing flicker updates; no shared material is modified.
        foreach(var light in blackoutLights)if(light)light.enabled=false;
        for(int i=0;i<tubeStates.Length;i++)if(blackoutTubes[i])blackoutTubes[i].SetPropertyBlock(darkTube);
    }
    void RestoreCircuit()
    {
        if(!BlackoutActive)return;
        for(int i=0;i<lightStates.Length;i++)if(blackoutLights[i])blackoutLights[i].enabled=lightStates[i];
        for(int i=0;i<tubeStates.Length;i++)if(blackoutTubes[i])blackoutTubes[i].SetPropertyBlock(tubeStates[i]);
        BlackoutActive=false;
    }
    void OnDisable(){StopAllCoroutines();pendingSpawns=0;RestoreCircuit();}
#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if(waves==null)return;
        var previous=Gizmos.matrix;Gizmos.matrix=transform.localToWorldMatrix;
        foreach(var wave in waves)
        {
            Gizmos.color=wave.afterKeyPickup?new Color(.9f,.25f,.1f):new Color(.9f,.65f,.1f);
            if(!wave.afterKeyPickup)Gizmos.DrawWireCube(wave.activationBounds.center,wave.activationBounds.size);
            if(wave.points==null)continue;
            foreach(var point in wave.points)if(point)Gizmos.DrawWireSphere(transform.InverseTransformPoint(point.position),.5f);
        }
        Gizmos.matrix=previous;
    }
#endif
}
