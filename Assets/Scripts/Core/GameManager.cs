using UnityEngine;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab; // Player prefab
    [SerializeField] private Transform spawnPoint; // Where the player spawns
    
    [SerializeField] private GameObject crosshairPrefab; // drag the "CrossHair" prefab (the entire Canvas)

    void Start()
    {
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        Instantiate(crosshairPrefab);

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
