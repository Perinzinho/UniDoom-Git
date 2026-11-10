using UnityEngine;

/// <summary>The randomized library access key; independent of prison and maintenance keys.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
public sealed class LawSchoolCellKeyPickup : MonoBehaviour
{
    [SerializeField] string cellId = CampusLibraryAccess.LawSchoolKeyId;
    [SerializeField] LawSchoolAlarmPrototype alarm;
    bool collected;

    public string CellId => cellId;
    public bool Collected => collected;

    void Start()
    {
        var player = GameManager.SessionPlayer;
        var keys = player ? player.GetComponent<PlayerCellKeys>() : null;
        if (keys && keys.HasKey(cellId)) HideCollectedKey();
    }

    void OnTriggerEnter(Collider other) => TryCollect(other);
    void OnTriggerStay(Collider other) => TryCollect(other);

    bool TryCollect(Collider other)
    {
        if (collected || !alarm || !alarm.isActiveAndEnabled || Time.timeScale <= 0 ||
            UniDoom.UI.GameMenus.BlocksGameplay) return false;
        var player = other.GetComponentInParent<PlayerPickupKey>();
        if (!player) return false;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return false;
        var keys = player.GetComponent<PlayerCellKeys>();
        if (!keys) keys = player.gameObject.AddComponent<PlayerCellKeys>();
        bool newlyCollected = keys.TryAddKey(cellId);
        HideCollectedKey();
        if (newlyCollected) SFXManager.Instance?.PlayKeyPickupSound();
        if (newlyCollected)
        {
            LawSchoolAlarmPrototype.NotifyKeyCollected(this, keys);
        }
        return newlyCollected;
    }

    void HideCollectedKey()
    {
        collected = true;
        gameObject.SetActive(false);
    }
}
