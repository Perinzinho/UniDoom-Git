using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>One numbered teacher-cell key in the existing persistent player inventory.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(SphereCollider))]
public sealed class CellKeyPickup : MonoBehaviour
{
    [SerializeField, Range(1, PrisonProgress.CellCount)] int cellId = 1;
    [SerializeField] bool showNumberLabel;
    bool collected;
    public int CellId => cellId;

    void Awake()
    {
        GetComponent<SphereCollider>().isTrigger = true;
        if (!showNumberLabel) return;
        var image = GetComponentInChildren<SpriteRenderer>();
        if (!image) return;
        // Reuse the existing key art; the number follows its billboard on the tag.
        var labelObject = new GameObject("Cell key number", typeof(TextMeshPro));
        labelObject.transform.SetParent(image.transform, false);
        labelObject.transform.localPosition = new Vector3(.35f, 1.1f, -.03f);
        labelObject.transform.localRotation = Quaternion.Euler(0, 0, -15);
        var label = labelObject.GetComponent<TextMeshPro>();
        label.text = cellId.ToString("00");
        label.fontSize = 10;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(.95f, .91f, .8f, 1);
        label.rectTransform.sizeDelta = new Vector2(2.4f, 1f);
        var labelRenderer = label.GetComponent<MeshRenderer>();
        labelRenderer.sortingLayerID = image.sortingLayerID;
        labelRenderer.sortingOrder = image.sortingOrder + 1;
    }

    IEnumerator Start()
    {
        while (GameManager.SessionPlayer == null) yield return null;
        var keys = GameManager.SessionPlayer.GetComponent<PlayerCellKeys>();
        if (PrisonProgress.HasCellKey(cellId, keys)) Hide();
    }

    void OnTriggerEnter(Collider other) => TryCollect(other);
    void OnTriggerStay(Collider other) => TryCollect(other);

    void TryCollect(Collider other)
    {
        if (collected || Time.timeScale <= 0 || UniDoom.UI.GameMenus.BlocksGameplay ||
            UniDoom.UI.DeathScreen.IsVisible || UniDoom.UI.DoorReloadScreen.IsReloading) return;
        var player = other.GetComponentInParent<PlayerPickupKey>();
        if (!player || !GameManager.SessionPlayer || player.gameObject != GameManager.SessionPlayer.gameObject) return;
        var life = player.GetComponent<PlayerLife>();
        if (life && life.IsDead) return;
        var keys = player.GetComponent<PlayerCellKeys>();
        if (!keys) keys = player.gameObject.AddComponent<PlayerCellKeys>();
        if (keys.TryAddKey(PrisonProgress.CellKeyId(cellId))) SFXManager.Instance?.PlayKeyPickupSound();
        Hide();
    }

    void Hide() { collected = true; gameObject.SetActive(false); }
}
