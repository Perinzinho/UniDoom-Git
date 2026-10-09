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

    public Gun GunPrefab => gunPrefab;
    public WeaponPickup PickupPrefab => pickupPrefab;
    public Sprite[] IdleFrames => idleFrames;
    public Sprite[] ShootFrames => shootFrames;
    public Sprite[] RechargeFrames => rechargeFrames;
    public float IdleFrameRate => idleFrameRate;
    public float ShootFrameRate => shootFrameRate;
    public float RechargeFrameRate => rechargeFrameRate;
}
