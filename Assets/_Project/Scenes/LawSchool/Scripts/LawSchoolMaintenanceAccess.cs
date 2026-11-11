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
    [SerializeField, Min(.5f)] float warningDistance = 2.5f;
    const string WarningText = "A porta está trancada";
    GUIStyle warningStyle;
    public bool WarningVisible { get; private set; }

    void Awake() { if (entranceBarrier) entranceBarrier.enabled = true; }
    void Update()
    {
        WarningVisible = false;
        var player = GameManager.SessionPlayer;
        var inventory = player ? player.GetComponent<PlayerCellKeys>() : null;
        bool locked = !inventory || !inventory.HasKey(KeyId);
        if (entranceBarrier) entranceBarrier.enabled = locked;
        if (!locked || !entranceBarrier || !player || Time.timeScale <= 0 ||
            UniDoom.UI.GameMenus.BlocksGameplay || UniDoom.UI.DeathScreen.IsVisible ||
            UniDoom.UI.DoorReloadScreen.IsReloading ||
            gameObject.scene != UnityEngine.SceneManagement.SceneManager.GetActiveScene()) return;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return;
        var controller = player.GetComponent<CharacterController>();
        Vector3 position = controller ? controller.bounds.center : player.transform.position;
        WarningVisible = entranceBarrier.bounds.SqrDistance(position) <= warningDistance * warningDistance;
    }

    void OnGUI()
    {
        if (!WarningVisible || !entranceBarrier || !entranceBarrier.enabled) return;
        warningStyle ??= new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            fontSize = Mathf.RoundToInt(19 * Mathf.Clamp(Screen.height / 900f, .8f, 1.6f))
        };
        float width = Mathf.Min(520, Screen.width - 40);
        GUI.Box(new Rect((Screen.width - width) * .5f, Screen.height * .76f, width, 52), WarningText, warningStyle);
    }

    bool IsDistant(Transform point)
    {
        if (!alarm || !alarm.SelectedKeyLocation || !IsEligible(point, alarm.SelectedKeyRoom)) return false;
        Vector3 delta = point.position - alarm.SelectedKeyLocation.position;
        delta.y = 0;
        return delta.sqrMagnitude >= minimumKeySeparation * minimumKeySeparation;
    }

    void OnEnable() { if (alarm) alarm.StateChanged += Sync; }
    void OnDisable() { WarningVisible = false; if (alarm) alarm.StateChanged -= Sync; }
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
