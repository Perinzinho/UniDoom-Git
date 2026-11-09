using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>The alarm exposes one maintenance key; room choice and ownership survive scene returns.</summary>
[DisallowMultipleComponent]
public sealed class LawSchoolMaintenanceAccess : MonoBehaviour
{
    public const string KeyId = "law-school-maintenance";
    [SerializeField] LawSchoolAlarmPrototype alarm;
    [SerializeField] LawSchoolMaintenanceKeyPickup key;
    [SerializeField] Transform[] locations;

    void OnEnable() { if (alarm) alarm.StateChanged += Sync; }
    void OnDisable() { if (alarm) alarm.StateChanged -= Sync; }
    IEnumerator Start()
    {
        while (!GameManager.SessionPlayer) yield return null;
        Sync();
    }

    void Sync()
    {
        if (!key) return;
        var player = GameManager.SessionPlayer;
        var inventory = player ? player.GetComponent<PlayerCellKeys>() : null;
        bool visible = alarm && alarm.IsActive && inventory && !inventory.HasKey(KeyId);
        if (visible)
        {
            var choices = new List<string>();
            if (locations != null)
                foreach (var point in locations)
                    if (IsEligible(point, alarm.SelectedKeyRoom) && !choices.Contains(point.name)) choices.Add(point.name);
            string chosen = inventory.GetOrChooseKeyLocation(KeyId, choices);
            Transform selected = null;
            if (locations != null)
                foreach (var point in locations)
                    if (IsEligible(point, alarm.SelectedKeyRoom) && point.name == chosen) { selected = point; break; }
            visible = selected;
            if (selected) key.transform.position = selected.position;
        }
        key.gameObject.SetActive(visible);
    }

    public static bool IsEligible(Transform point, string alarmKeyRoom) =>
        point && point.parent && point.parent.name != alarmKeyRoom;
}
