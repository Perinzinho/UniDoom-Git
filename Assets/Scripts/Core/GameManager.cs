using UnityEngine;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab; // Player prefab
    [SerializeField] private Transform spawnPoint; // Where the player spawns
    
    [SerializeField] private GameObject crosshairPrefab; // drag the "CrossHair" prefab (the entire Canvas)

    [Tooltip("Optional: 6 frames of life_sprt.png. Auto-loaded in the editor if empty.")]
    [SerializeField] private Sprite[] heartBeatingFrames;

    void Start()
    {
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        Instantiate(crosshairPrefab);

        PlayerLife playerLife = playerInstance.GetComponent<PlayerLife>();
        HeartAnimator heartbeat = new GameObject("HealthHeart").AddComponent<HeartAnimator>();
        heartbeat.Init(playerLife);
        if (heartBeatingFrames != null && heartBeatingFrames.Length > 0)
        {
            heartbeat.SetHeartFrames(heartBeatingFrames);
        }
        DebugUI.Log($"playerLife found: {playerLife != null}");

        Pistol playerGun = playerInstance.GetComponentInChildren<Pistol>();
        WeaponSpriteAnimator weaponAnimator = FindObjectOfType<WeaponSpriteAnimator>();

        DebugUI.Log($"playerGun found: {playerGun != null}");
        DebugUI.Log($"weaponAnimator found: {weaponAnimator != null}");

        if (weaponAnimator != null && playerGun != null)
        {
            weaponAnimator.SetGun(playerGun);
            DebugUI.Log("SetGun called successfully!");
        }
        else
        {
            DebugUI.LogWarning("GameManager: could not connect weapon to sprite animator.");
        }
    }
}
