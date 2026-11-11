using UnityEngine;

namespace UniDoom.Utils
{
    public static class CampusGrounding
    {
        static RaycastHit[] floorHits = new RaycastHit[32];

        public static bool TryFloor(Vector3 feet, out RaycastHit floor)
        {
            Vector3 origin = feet + Vector3.up * .8f;
            int count;
            // A full buffer does not prove that all hits were returned. Grow and query again.
            while ((count = Physics.RaycastNonAlloc(origin, Vector3.down, floorHits, 16,
                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) == floorHits.Length)
                System.Array.Resize(ref floorHits, checked(floorHits.Length * 2));

            bool found = SelectFloor(floorHits, count, out floor, out bool tied);
            if (!tied) return found;

            // Preserve the original first-hit choice for equally close valid supports.
            // NonAlloc does not promise the same ordering as RaycastAll in that case.
            var originalHits = Physics.RaycastAll(origin, Vector3.down, 16,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            return SelectFloor(originalHits, originalHits.Length, out floor, out _);
        }

        static bool SelectFloor(RaycastHit[] hits, int count, out RaycastHit floor, out bool tied)
        {
            floor = default;
            tied = false;
            float nearest = float.PositiveInfinity;
            for (int i = 0; i < count; i++)
            {
                var hit = hits[i];
                var col = hit.collider;
                if (col.gameObject.scene.name != "CampusExterior" || hit.normal.y < .65f ||
                    col.GetComponentInParent<IDamageable>() != null || col.GetComponentInParent<PlayerMovement>() != null) continue;
                string name = col.name.ToLowerInvariant();
                var size = col.bounds.size;
                bool ground = name.Contains("ground") || name.Contains("asphalt") || name.Contains("sidewalk") ||
                    name.Contains("pavement") || name.Contains("terrain") || name.Contains("floor") ||
                    (size.y < 1.2f && size.x > 2 && size.z > 2);
                if (!ground) continue;
                if (hit.distance == nearest) { tied = true; continue; }
                if (hit.distance >= nearest) continue;
                nearest = hit.distance;
                floor = hit;
                tied = false;
            }
            return nearest < float.PositiveInfinity;
        }

        public static bool TryArrival(PlayerMovement player, Vector3 authored, out Vector3 position)
        {
            position = authored;
            var controller = player.GetComponent<CharacterController>();
            if (!controller) return false;
            Vector3 scale = player.transform.lossyScale;
            float half = controller.height * Mathf.Abs(scale.y) * .5f;
            float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
            Vector3 center = player.transform.TransformVector(controller.center);
            float rootToFeet = half - center.y;
            for (int ring = 0; ring <= 4; ring++)
            {
                int samples = ring == 0 ? 1 : 16;
                for (int i = 0; i < samples; i++)
                {
                    float angle = i * Mathf.PI * 2 / samples;
                    var candidate = authored + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * ring;
                    if (!TryFloor(candidate - Vector3.up * rootToFeet, out var floor)) continue;
                    candidate.y = floor.point.y + rootToFeet + controller.skinWidth + .025f;
                    var middle = candidate + center;
                    var axis = Vector3.up * Mathf.Max(0, half - radius);
                    bool blocked = false;
                    foreach (var col in Physics.OverlapCapsule(middle - axis, middle + axis,
                                 Mathf.Max(.05f, radius - .02f), Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
                    {
                        if (col.transform.IsChildOf(player.transform)) continue;
                        if (!col.isTrigger || col.GetComponent<SceneTransitionDoor>()) { blocked = true; break; }
                    }
                    if (blocked) continue;
                    position = candidate;
                    return true;
                }
            }
            return false;
        }
    }
}
