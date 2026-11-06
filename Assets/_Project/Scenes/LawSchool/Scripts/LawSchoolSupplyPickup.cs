using UnityEngine;

/// <summary>Records successful native pickup collection without changing health/ammo behavior.</summary>
[DisallowMultipleComponent]
public sealed class LawSchoolSupplyPickup : MonoBehaviour
{
    LawSchoolSupplyState state;
    string roomId;
    MedkitPickup medkit;
    AmmoPickup ammo;
    public string RoomId => roomId;

    public void Initialize(LawSchoolSupplyState owner,string room) { state = owner; roomId = room; }
    void Awake() { medkit = GetComponent<MedkitPickup>(); ammo = GetComponent<AmmoPickup>(); }
    void OnEnable()
    {
        if (medkit) medkit.Collected += RecordCollection;
        if (ammo) ammo.Collected += RecordCollection;
    }
    void OnDisable()
    {
        if (medkit) medkit.Collected -= RecordCollection;
        if (ammo) ammo.Collected -= RecordCollection;
    }
    void RecordCollection() { if (state) state.MarkCollected(roomId); }
}
