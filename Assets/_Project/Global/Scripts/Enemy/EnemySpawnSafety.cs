using UnityEngine;

/// <summary>Runtime reinforcements must enter outside the player's view and personal space.</summary>
public static class EnemySpawnSafety
{
    // Shared scratch buffers: all callers run synchronously on Unity's main thread.
    static readonly Plane[] planes = new Plane[6];
    static readonly RaycastHit[] hits = new RaycastHit[64];

    public static bool CanSpawn(Vector3 position, Transform player, float minimumDistance = 6f)
    {
        if (player == null && GameManager.SessionPlayer != null)
            player = GameManager.SessionPlayer.transform;
        Camera camera = Camera.main;
        // A missing camera/player during a transition is not permission to spawn in view.
        if (player == null || camera == null || !camera.isActiveAndEnabled) return false;
        Vector3 distance = position - player.position;
        distance.y = 0f;
        if (distance.sqrMagnitude < Mathf.Pow(Mathf.Max(6f, minimumDistance), 2f)) return false;

        // Conservative envelope includes the tall/wide alternate zombie sprites and markers
        // authored at either the feet or the collider centre. Check more than the pivot:
        // a desk hiding the feet must not allow a visible head to materialise above it.
        Bounds bounds = new Bounds(position + Vector3.up * .5f, new Vector3(3f, 4f, 3f));
        GeometryUtility.CalculateFrustumPlanes(camera, planes);
        if (!GeometryUtility.TestPlanesAABB(planes, bounds)) return true;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
                for (int z = -1; z <= 1; z++)
                {
                    Vector3 sample = bounds.center + Vector3.Scale(bounds.extents, new Vector3(x, y, z));
                    if (!Occluded(camera.transform.position, sample, player)) return false;
                }
        return true;
    }

    static bool Occluded(Vector3 origin, Vector3 sample, Transform player)
    {
        Vector3 ray = sample - origin;
        if (ray.sqrMagnitude < .001f) return false;
        int count = Physics.RaycastNonAlloc(origin, ray.normalized, hits, ray.magnitude,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        // Overflow cannot establish which objects blocked the ray safely.
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++)
        {
            Transform hit = hits[i].transform;
            if (hit.IsChildOf(player) || hits[i].collider.GetComponentInParent<Enemy>() != null) continue;
            return true;
        }
        return false;
    }
}
