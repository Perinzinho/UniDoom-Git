using UnityEngine;

/// <summary>The library access key inside the auditorium.</summary>
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
        if (keys) RefreshAvailability(keys);
    }

    internal void RefreshAvailability(PlayerCellKeys inventory)
    {
        collected = inventory.HasKey(cellId);
        gameObject.SetActive(!collected);
    }

    void OnTriggerEnter(Collider other) => TryCollect(other);
    void OnTriggerStay(Collider other) => TryCollect(other);

    bool TryCollect(Collider other)
    {
        if (collected || !alarm || !alarm.isActiveAndEnabled || !alarm.IsResolved || Time.timeScale <= 0 ||
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
            UniDoom.UI.GameplayNotice.Show("Chave da Biblioteca do Campus obtida. A entrada precisa de duas chaves. Já posso sair do Bloco de Direito.", 7.5f);
        }
        return newlyCollected;
    }

    void HideCollectedKey()
    {
        collected = true;
        gameObject.SetActive(false);
    }
}
