using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class MedkitPickup : MonoBehaviour
{
    [Header("Heal Settings")]
    [SerializeField] private int healAmount = 50;
    private bool consumed;
    private string sessionId;
    private bool runtimeDrop;
    private static readonly HashSet<string> collected = new HashSet<string>();

    public static void ResetSession() => collected.Clear();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => ResetSession();

    private void Awake()
    {
        // Names and authored positions survive sibling removals and the persistent
        // GameManager leaving the scene. Never use instance IDs or sibling indices.
        sessionId = "";
        for (Transform node = transform; node != null; node = node.parent)
        {
            Vector3 p = node.localPosition;
            sessionId = System.FormattableString.Invariant($"/{node.name}@{p.x:R},{p.y:R},{p.z:R}") + sessionId;
        }
        sessionId = gameObject.scene.path + sessionId;
    }

    private void Start()
    {
        if (!runtimeDrop && collected.Contains(sessionId))
        {
            consumed = true;
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    // Enemy drops are new items, not authored room pickups. Called before Start.
    public void MarkAsRuntimeDrop() => runtimeDrop = true;

    private void OnTriggerEnter(Collider other)
    {
        TryCollect(other);
    }

    public bool TryCollect(Collider other)
    {
        if (consumed || other == null || healAmount <= 0 || UniDoom.UI.DeathScreen.IsVisible)
            return false;

        PlayerLife life = other.GetComponentInParent<PlayerLife>();
        if (life == null || !life.CompareTag("Player") || life.IsDead || life.CurrentHealth >= life.MaxHealth)
            return false;

        // The player's multiple colliders may enter during the same physics step.
        consumed = true;
        if (!runtimeDrop) collected.Add(sessionId);
        foreach (Collider trigger in GetComponents<Collider>())
            trigger.enabled = false;
        life.Heal(healAmount);
        GameplayAudio.Instance?.PlayMedkitPickup();
        Destroy(gameObject);
        return true;
    }
}
