using UnityEngine;

// Shared configuration; ammunition remains on each individual Gun instance.
[CreateAssetMenu(menuName = "Weapons/Weapon Definition")]
public class WeaponDefinition : ScriptableObject
{
    [SerializeField] private Gun gunPrefab;
    [SerializeField] private WeaponPickup pickupPrefab;
    [SerializeField] private Sprite[] idleFrames;
    [SerializeField] private Sprite[] shootFrames;
    [SerializeField] private Sprite[] rechargeFrames;
    [SerializeField] private float idleFrameRate = 12f;
    [SerializeField] private float shootFrameRate = 20f;
    [SerializeField] private float rechargeFrameRate = 5f;

    [Header("HUD")]
    [Tooltip("Offset from the weapon HUD's default position, in canvas units.")]
    [SerializeField] private Vector2 hudOffset;
    [Tooltip("Optional HUD size in canvas units. Zero keeps the prefab's default size.")]
    [SerializeField] private Vector2 hudSize;

    public Gun GunPrefab => gunPrefab;
    public WeaponPickup PickupPrefab => pickupPrefab;
    public Sprite[] IdleFrames => idleFrames;
    public Sprite[] ShootFrames => shootFrames;
    public Sprite[] RechargeFrames => rechargeFrames;
    public float IdleFrameRate => idleFrameRate;
    public float ShootFrameRate => shootFrameRate;
    public float RechargeFrameRate => rechargeFrameRate;
    public Vector2 HudOffset => hudOffset;
    public Vector2 HudSize => hudSize;
}
