using UnityEngine;

// Shared configuration; ammunition remains on each individual Gun instance.
[CreateAssetMenu(menuName = "Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [System.Serializable]
    public struct HudFrameBounds
    {
        public Sprite sprite;
        public Rect opaqueRect;
    }
    [SerializeField] private Gun gunPrefab;
    [SerializeField] private Axe axePrefab;
    [SerializeField] private WeaponPickup pickupPrefab;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] shootFrames;
    [SerializeField] private Sprite[] rechargeFrames;
    [SerializeField] private float idleFrameRate = 12f;
    [SerializeField] private float shootFrameRate = 20f;
    [Tooltip("Optional duration of each attack pose. Empty uses Shoot Frame Rate.")]
    [SerializeField] private float[] shootFrameDurations;
    [SerializeField] private float rechargeFrameRate = 5f;

    [Header("HUD")]
    [Tooltip("Offset from the weapon HUD's default position, in canvas units.")]
    [SerializeField] private Vector2 hudOffset;
    [Tooltip("Optional HUD size in canvas units. Zero keeps the prefab's default size.")]
    [SerializeField] private Vector2 hudSize;
    [Tooltip("Maximum fraction of the viewport occupied by the complete animation canvas.")]
    [SerializeField] private Vector2 hudViewportLimit = new Vector2(.36f, .42f);
    [Tooltip("Attach cropped first-person artwork to the bottom/right screen edges.")]
    [SerializeField] private bool hudAttachToScreenEdges;
    [Tooltip("Optional opaque bounds in source texture pixels. Source sprites and importers stay unchanged.")]
    [SerializeField] private HudFrameBounds[] hudFrameBounds;

    public Gun GunPrefab => gunPrefab;
    public Axe AxePrefab => axePrefab;
    public WeaponPickup PickupPrefab => pickupPrefab;
    public Sprite[] IdleFrames => idleFrames;
    public Sprite[] ShootFrames => shootFrames;
    public Sprite[] RechargeFrames => rechargeFrames;
    public float IdleFrameRate => idleFrameRate;
    public float ShootFrameRate => shootFrameRate;
    public float[] ShootFrameDurations => shootFrameDurations;
    public float RechargeFrameRate => rechargeFrameRate;
    public Vector2 HudOffset => hudOffset;
    public Vector2 HudSize => hudSize;
    public bool HudAttachToScreenEdges => hudAttachToScreenEdges;
    public Rect HudFrameRect(Sprite sprite)
    {
        if (!sprite) return default;
        if (hudFrameBounds != null)
            foreach (var frame in hudFrameBounds)
                if (frame.sprite == sprite && frame.opaqueRect.width > 0 && frame.opaqueRect.height > 0)
                    return frame.opaqueRect;
        return sprite.rect;
    }
    public Vector2 HudViewportLimit => hudViewportLimit.x > 0 && hudViewportLimit.y > 0
        ? hudViewportLimit : new Vector2(.36f, .42f);
}
