using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Limited room supplies, shuffled once per run and retained across scene travel.</summary>
[DisallowMultipleComponent,DefaultExecutionOrder(1000)]
public sealed class LawSchoolRoomSupplies : MonoBehaviour
{
    [SerializeField] Transform[] roomPoints;
    [SerializeField] GameObject medkitPrefab;
    [SerializeField] GameObject ammoPrefab;
    [SerializeField,Min(0)] int medkitRooms = 6;
    [SerializeField,Min(0)] int ammoRooms = 10;
    public bool IsReady { get; private set; }

    IEnumerator Start()
    {
        while (!GameManager.SessionPlayer) yield return null;
        var player = GameManager.SessionPlayer;
        var state = player.GetComponent<LawSchoolSupplyState>();
        if (!state) state = player.gameObject.AddComponent<LawSchoolSupplyState>();
        var ids = new List<string>();
        if (roomPoints != null) foreach (var point in roomPoints) if (point) ids.Add(point.name);
        state.Initialize(ids,medkitRooms,ammoRooms);
        if (roomPoints != null) foreach (var point in roomPoints)
        {
            if (!point || state.WasCollected(point.name)) continue;
            var kind = state.KindFor(point.name);
            var prefab = kind == LawSchoolSupplyKind.Medkit ? medkitPrefab : kind == LawSchoolSupplyKind.Ammo ? ammoPrefab : null;
            if (!prefab) continue;
            var item = Instantiate(prefab,point.position,Quaternion.identity);
            item.transform.SetParent(transform,true); // Preserve pickup size independently of the building's 1.25 scale.
            item.name = point.name+" — "+kind;
            item.GetComponent<LawSchoolSupplyPickup>().Initialize(state,point.name);
        }
        IsReady = true;
    }
}
