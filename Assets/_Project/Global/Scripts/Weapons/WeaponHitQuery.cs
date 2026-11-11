using UnityEngine;

/// <summary>Finds the first solid weapon contact outside the attacking player.</summary>
internal static class WeaponHitQuery
{
    public static bool TryRaycast(Ray ray, float distance, Transform owner, out RaycastHit hit,
        int layerMask = Physics.DefaultRaycastLayers)
    {
        if (!Physics.Raycast(ray, out hit, distance, layerMask, QueryTriggerInteraction.Ignore)) return false;
        if (owner == null || !hit.collider.transform.IsChildOf(owner)) return true;

        // Camera offsets can put the player's capsule in front of the cast origin.
        // Only this case needs multiple hits; solid scenery still stops the attack.
        RaycastHit closest = default;
        float closestDistance = float.PositiveInfinity;
        bool found = false;
        foreach (var candidate in Physics.RaycastAll(ray, distance, layerMask, QueryTriggerInteraction.Ignore))
        {
            if (candidate.collider.transform.IsChildOf(owner) || candidate.distance >= closestDistance) continue;
            closest = candidate;
            closestDistance = candidate.distance;
            found = true;
        }
        hit = closest;
        return found;
    }
}
