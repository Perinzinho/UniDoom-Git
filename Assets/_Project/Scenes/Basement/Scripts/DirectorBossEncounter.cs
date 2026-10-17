using UnityEngine;
using UnityEngine.Events;

/// <summary>One Director per basement instance, spawned only after entering the final room.</summary>
public sealed class DirectorBossEncounter : MonoBehaviour
{
    [SerializeField] BoxCollider activationVolume;
    [SerializeField] Transform spawnPoint;
    [SerializeField] DirectorBoss bossPrefab;
    [SerializeField, Min(1)] float playerHeightMultiplier = 1.9f;
    public UnityEvent onEncounterStarted = new UnityEvent();
    public UnityEvent onEncounterCompleted = new UnityEvent();
    public bool Started { get; private set; }
    public bool Completed { get; private set; }
    public DirectorBoss SpawnedBoss { get; private set; }
    Transform player;

    void Awake()
    {
        if (activationVolume == null || spawnPoint == null || bossPrefab == null)
        {
            Debug.LogError("Director encounter needs its room volume, spawn point and boss prefab.", this);
            enabled = false;
        }
    }
    void Update()
    {
        if (Started) return;
        if (player == null)
        {
            var found = GameObject.FindGameObjectWithTag("Player");
            if (found != null) player = found.transform;
        }
        if (player != null && Contains(player.position)) BeginEncounter();
    }
    public bool Contains(Vector3 position) => activationVolume != null &&
        new Bounds(activationVolume.center, activationVolume.size).Contains(activationVolume.transform.InverseTransformPoint(position));

    public void BeginEncounter()
    {
        if (Started || !enabled || player == null) return;
        float playerHeight = 0;
        foreach (var collider in player.GetComponentsInChildren<Collider>())
            if (!collider.isTrigger) playerHeight = Mathf.Max(playerHeight, collider.bounds.size.y);
        var controller = bossPrefab.GetComponent<CharacterController>();
        float scale = controller != null && playerHeight > 0
            ? Mathf.Max(1, playerHeight * playerHeightMultiplier / controller.height) : 1;
        if (!FindSpawnPosition(controller, scale, out Vector3 position))
        {
            Debug.LogError("Director has no clear floor near FinalBossSpawn. Check room geometry and boss clearance.", this);
            enabled = false;
            return;
        }
        Started = true;
        SpawnedBoss = Instantiate(bossPrefab, position, spawnPoint.rotation);
        // Retain world dimensions even in scaled/rotated instances of the basement prefab.
        SpawnedBoss.transform.localScale = Vector3.one * scale;
        SpawnedBoss.transform.SetParent(transform, true);
        SpawnedBoss.name = "Diretor UniDoom — Boss";
        SpawnedBoss.SetTarget(player);
        SpawnedBoss.onDeathAnimationFinished.AddListener(CompleteEncounter);
        onEncounterStarted.Invoke();
    }
    bool FindSpawnPosition(CharacterController controller, float scale, out Vector3 position)
    {
        Physics.SyncTransforms();
        float radius = controller != null ? controller.radius * scale : .8f;
        float height = controller != null ? controller.height * scale : 4.6f;
        for (int ring = 0; ring <= 3; ring++)
        {
            int samples = ring == 0 ? 1 : 12;
            for (int sample = 0; sample < samples; sample++)
            {
                float angle = sample * Mathf.PI * 2 / samples;
                Vector3 candidate = spawnPoint.position + new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * (ring * 2.2f);
                if (!Physics.Raycast(candidate + Vector3.up * .2f, Vector3.down, out var floor, 6, ~0, QueryTriggerInteraction.Ignore)) continue;
                position = floor.point + Vector3.up * .06f;
                if (!Contains(position + Vector3.up)) continue;
                if (!Physics.CheckCapsule(position + Vector3.up * (radius + .08f), position + Vector3.up * (height - radius),
                    radius, ~0, QueryTriggerInteraction.Ignore)) return true;
            }
        }
        position = spawnPoint.position;
        return false;
    }
    void CompleteEncounter()
    {
        if (Completed) return;
        Completed = true;
        onEncounterCompleted.Invoke();
    }
    void OnDrawGizmosSelected()
    {
        if (activationVolume == null) return;
        Gizmos.color = new Color(.8f, .05f, .05f, .6f);
        Gizmos.matrix = activationVolume.transform.localToWorldMatrix;
        Gizmos.DrawWireCube(activationVolume.center, activationVolume.size);
    }
}
