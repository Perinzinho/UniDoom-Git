using System.Collections.Generic;
using UnityEngine;

public enum LawSchoolSupplyKind { Empty, Medkit, Ammo }

/// <summary>One shuffled supply plan per player session, including collected items.</summary>
[DisallowMultipleComponent]
public sealed class LawSchoolSupplyState : MonoBehaviour
{
    readonly Dictionary<string,LawSchoolSupplyKind> rooms = new Dictionary<string,LawSchoolSupplyKind>();
    readonly HashSet<string> collected = new HashSet<string>();

    public void Initialize(IReadOnlyList<string> roomIds,int medkitRooms,int ammoRooms)
    {
        if (rooms.Count > 0 || roomIds == null) return;
        var shuffled = new List<string>();
        foreach (string id in roomIds) if (!string.IsNullOrEmpty(id) && !shuffled.Contains(id)) shuffled.Add(id);
        for (int i = shuffled.Count-1; i > 0; i--)
        {
            int other = Random.Range(0,i+1);
            string swap = shuffled[i]; shuffled[i] = shuffled[other]; shuffled[other] = swap;
        }
        int medkits = Mathf.Clamp(medkitRooms,0,shuffled.Count);
        int ammo = Mathf.Clamp(ammoRooms,0,shuffled.Count-medkits);
        for (int i = 0; i < shuffled.Count; i++)
            rooms.Add(shuffled[i],i < medkits ? LawSchoolSupplyKind.Medkit :
                i < medkits+ammo ? LawSchoolSupplyKind.Ammo : LawSchoolSupplyKind.Empty);
    }

    public LawSchoolSupplyKind KindFor(string roomId) => rooms.TryGetValue(roomId,out var kind) ? kind : LawSchoolSupplyKind.Empty;
    public bool WasCollected(string roomId) => collected.Contains(roomId);
    public void MarkCollected(string roomId) { if (rooms.ContainsKey(roomId)) collected.Add(roomId); }
}
