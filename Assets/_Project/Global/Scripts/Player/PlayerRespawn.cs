using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UniDoom.UI;
using UniDoom.Utils;

/// <summary>Session checkpoint: the initial spawn, replaced by each newly collected key.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(PlayerMovement), typeof(PlayerLife))]
public sealed class PlayerRespawn : MonoBehaviour
{
    private string savedScene;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private PlayerLife life;
    private Coroutine respawnRoutine;

    public bool HasKeyCheckpoint { get; private set; }

    private void Awake()
    {
        life = GetComponent<PlayerLife>();
    }

    private void OnEnable() => life.OnDeath += HandleDeath;
    private void OnDisable()
    {
        life.OnDeath -= HandleDeath;
        if (respawnRoutine != null) StopCoroutine(respawnRoutine);
        respawnRoutine = null;
    }

    public void Initialize(string scenePath, Vector3 position, Quaternion rotation)
    {
        savedScene = scenePath;
        savedPosition = position;
        savedRotation = rotation;
        HasKeyCheckpoint = false;
    }

    public void SaveKeyCheckpoint(Scene scene)
    {
        if (life.IsDead || DoorReloadScreen.IsReloading) return;
        // Save the player's feet at contact, rather than the key's floating visual position.
        savedScene = scene.path;
        savedPosition = transform.position;
        savedRotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        var cameraTransform = GetComponent<PlayerMovement>().cameraTransform;
        if (cameraTransform != null)
        {
            var forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up);
            if (forward.sqrMagnitude > .001f) savedRotation = Quaternion.LookRotation(forward);
        }
        HasKeyCheckpoint = true;
    }

    private void HandleDeath()
    {
        if (respawnRoutine == null) respawnRoutine = StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        // Leave the death event before starting scene travel; never race another transition.
        yield return null;
        while (DoorReloadScreen.IsReloading || MouseSensitivityMenu.IsOpen) yield return null;
        if (!DoorReloadScreen.TryRespawnScene(savedScene, () =>
        {
            SceneArrivalPoint.PlacePlayer(GetComponent<PlayerMovement>(), savedPosition, savedRotation);
            life.RestoreAfterRespawn();
        }))
            Debug.LogError($"[PlayerRespawn] Não foi possível retornar ao checkpoint em '{savedScene}'.");
        respawnRoutine = null;
    }
}
