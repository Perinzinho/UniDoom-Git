using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Two distant key rooms and a solid maintenance entrance lock.</summary>
[DisallowMultipleComponent]
public sealed class LawSchoolMaintenanceAccess : MonoBehaviour
{
    public const string KeyId = "law-school-maintenance";
    [SerializeField] LawSchoolAlarmPrototype alarm;
    [SerializeField] LawSchoolMaintenanceKeyPickup key;
    [SerializeField] Transform[] locations;
    [SerializeField] BoxCollider entranceBarrier;
    [SerializeField, Min(1f)] float minimumKeySeparation = 45f;

    void Awake() { if (entranceBarrier) entranceBarrier.enabled = true; }
    void Update()
    {
        var player = GameManager.SessionPlayer;
        var inventory = player ? player.GetComponent<PlayerCellKeys>() : null;
        if (entranceBarrier) entranceBarrier.enabled = !inventory || !inventory.HasKey(KeyId);
    }

    bool IsDistant(Transform point)
    {
        if (!IsEligible(point, alarm.SelectedKeyRoom)) return false;
        Transform alarmPoint = null;
        foreach (var candidate in locations)
            if (candidate && candidate.parent && candidate.parent.name == alarm.SelectedKeyRoom)
            { alarmPoint = candidate; break; }
        if (!alarmPoint) return false;
        Vector3 delta = point.position - alarmPoint.position;
        delta.y = 0;
        return delta.sqrMagnitude >= minimumKeySeparation * minimumKeySeparation;
    }

    void OnEnable() { if (alarm) alarm.StateChanged += Sync; }
    void OnDisable() { if (alarm) alarm.StateChanged -= Sync; }
    IEnumerator Start()
    {
        while (!GameManager.SessionPlayer) yield return null;
        while (alarm && string.IsNullOrEmpty(alarm.SelectedKeyRoom)) yield return null;
        Sync();
    }

    void Sync()
    {
        if (!key) return;
        var player = GameManager.SessionPlayer;
        var inventory = player ? player.GetComponent<PlayerCellKeys>() : null;
        bool visible = alarm && !string.IsNullOrEmpty(alarm.SelectedKeyRoom) && inventory && !inventory.HasKey(KeyId);
        if (visible)
        {
            var choices = new List<string>();
            if (locations != null)
                foreach (var point in locations)
                    if (IsDistant(point) && !choices.Contains(point.name)) choices.Add(point.name);
            string chosen = inventory.GetOrChooseKeyLocation(KeyId, choices);
            if (choices.Count == 0)
                Debug.LogError("No maintenance key room meets the required separation. Check the configured locations.", this);
            Transform selected = null;
            if (locations != null)
                foreach (var point in locations)
                    if (IsDistant(point) && point.name == chosen) { selected = point; break; }
            visible = selected;
            if (selected) key.transform.position = selected.position;
        }
        key.gameObject.SetActive(visible);
    }

    public static bool IsEligible(Transform point, string alarmKeyRoom) =>
        point && point.parent && point.parent.name != alarmKeyRoom;
}
