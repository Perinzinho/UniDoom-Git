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

    // Temporary map selector: relocate the scene's start before the player is instantiated.
    public bool TryConfigurePlayerStart(Transform start, CharacterController playerController)
    {
        if (start == null || activationVolume == null || spawnPoint == null || playerController == null) return false;
        Vector3 scale = playerController.transform.lossyScale;
        float radius = playerController.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(playerController.height * Mathf.Abs(scale.y), radius * 2);
        float clearance = Mathf.Max(.1f, playerController.skinWidth * Mathf.Abs(scale.y));
        Vector3 centerOffset = Vector3.Scale(playerController.center, scale);
        Physics.SyncTransforms();
        Vector3 entry = activationVolume.center;
        entry.z -= activationVolume.size.z * .5f - 3f;
        entry.y = activationVolume.center.y;
        for (int row = 0; row < 4; row++)
            for (int column = 0; column < 5; column++)
            {
                // Search the entrance side of the boss room, away from the boss's own spawn.
                float lateral = column == 0 ? 0 : ((column + 1) / 2) * 2f * (column % 2 == 0 ? -1 : 1);
                Vector3 candidate = activationVolume.transform.TransformPoint(entry + new Vector3(lateral, 0, row * 2));
                if (!Physics.Raycast(candidate, Vector3.down, out var floor, activationVolume.size.y + 2,
                    ~0, QueryTriggerInteraction.Ignore) || floor.normal.y < .7f) continue;
                if (Physics.GetIgnoreLayerCollision(playerController.gameObject.layer, floor.collider.gameObject.layer)) continue;
                if (!Contains(floor.point + Vector3.up)) continue;
                // Check and place the real, scaled capsule with its feet above the floor.
                if (Physics.CheckCapsule(floor.point + Vector3.up * (clearance + radius),
                    floor.point + Vector3.up * (clearance + height - radius),
                    radius, ~0, QueryTriggerInteraction.Ignore)) continue;
                Vector3 direction = spawnPoint.position - floor.point;
                direction.y = 0;
                Quaternion rotation = direction.sqrMagnitude > .001f ? Quaternion.LookRotation(direction) : Quaternion.identity;
                start.SetPositionAndRotation(floor.point + Vector3.up * (height * .5f + clearance) - rotation * centerOffset,
                    rotation);
                return true;
            }
        return false;
    }

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
        GameplayAudio.Instance?.StartBossMusic(SpawnedBoss);
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
