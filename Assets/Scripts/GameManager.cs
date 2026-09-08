using UnityEngine;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab; // Prefab do Player
    [SerializeField] private Transform spawnPoint; // Onde o player spawna
    
    [SerializeField] private GameObject crosshairPrefab; // arraste o prefab "CrossHair" (o Canvas inteiro)

    void Start()
    {
        GameObject playerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        Instantiate(crosshairPrefab);

        Pistol playerGun = playerInstance.GetComponentInChildren<Pistol>();
        WeaponSpriteAnimator weaponAnimator = FindObjectOfType<WeaponSpriteAnimator>();

        DebugUI.Log($"playerGun encontrado: {playerGun != null}");
        DebugUI.Log($"weaponAnimator encontrado: {weaponAnimator != null}");

        if (weaponAnimator != null && playerGun != null)
        {
            weaponAnimator.SetGun(playerGun);
            DebugUI.Log("SetGun foi chamado com sucesso!");
        }
        else
        {
            DebugUI.LogWarning("GameManager: não foi possível conectar a arma ao animador do sprite.");
        }
    }
}