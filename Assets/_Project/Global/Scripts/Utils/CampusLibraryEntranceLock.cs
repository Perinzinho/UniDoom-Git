using TMPro;
using UnityEngine;
using UniDoom.Utils;

/// <summary>Two independent library locks, backed by the existing persistent player inventory.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SceneTransitionDoor))]
public sealed class CampusLibraryEntranceLock : MonoBehaviour, ISceneTransitionGuard
{
    [SerializeField] BoxCollider solidBarrier;
    [SerializeField] TMP_Text status;
    [SerializeField] Renderer lawSchoolIndicator, block4Indicator;
    [SerializeField] SpriteRenderer lawSchoolKeyIcon, block4KeyIcon;
    [SerializeField] GameObject lockingBar;
    PlayerCellKeys lastInventory;
    int lastRevision = -1;
    MaterialPropertyBlock colors;

    public bool IsTransitionBlocked => !CampusLibraryAccess.CanEnter(Inventory);
    static PlayerCellKeys Inventory => GameManager.SessionPlayer ?
        GameManager.SessionPlayer.GetComponent<PlayerCellKeys>() : null;

    void OnEnable() => Refresh(true);
    void Update() => Refresh(false);

    void Refresh(bool force)
    {
        var inventory = Inventory;
        int revision = inventory ? inventory.Revision : -1;
        bool blocked = !CampusLibraryAccess.CanEnter(inventory);
        if (solidBarrier) solidBarrier.enabled = blocked;
        if (lockingBar) lockingBar.SetActive(blocked);
        if (!force && inventory == lastInventory && revision == lastRevision) return;
        lastInventory = inventory;
        lastRevision = revision;
        bool law = inventory && inventory.HasKey(CampusLibraryAccess.LawSchoolKeyId);
        bool block4 = inventory && inventory.HasKey(CampusLibraryAccess.Block4KeyId);
        Indicator(lawSchoolIndicator, law);
        Indicator(block4Indicator, block4);
        if (lawSchoolKeyIcon) lawSchoolKeyIcon.color = law ? Color.white : new Color(.70f,.70f,.70f,.75f);
        if (block4KeyIcon) block4KeyIcon.color = block4 ? Color.white : new Color(.70f,.70f,.70f,.75f);
        if (status) status.text = blocked ? "BIBLIOTECA\nDUAS CHAVES NECESSÁRIAS  •  " + CampusLibraryAccess.KeyCount(inventory) + "/2" : "BIBLIOTECA\nACESSO LIBERADO  •  2/2";
    }

    void Indicator(Renderer indicator, bool available)
    {
        if (!indicator) return;
        colors ??= new MaterialPropertyBlock();
        indicator.GetPropertyBlock(colors);
        Color color = available ? new Color(.26f,.65f,.34f) : new Color(.72f,.24f,.12f);
        colors.SetColor("_BaseColor", color);
        colors.SetColor("_Color", color);
        indicator.SetPropertyBlock(colors);
    }
}
