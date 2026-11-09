using System.Collections.Generic;
using UnityEngine;

namespace UniDoom.Utils
{
    public interface ISceneTransitionGuard
    {
        bool IsTransitionBlocked { get; }
    }

    /// <summary>Touch volume at a doorway. Arrivals must leave the volume before using it again.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(BoxCollider))]
    public sealed class SceneTransitionDoor : MonoBehaviour
    {
        [SerializeField] private string destinationScene;
        [SerializeField] private string destinationSpawnId;
        [SerializeField] private string destinationLabel;
        [SerializeField] private bool requiresKey01, requiresKey02, requiresKey03;
        [SerializeField] private OpenDoorWithKey physicalDoor;
        [SerializeField] private bool waitForPhysicalDoorToOpen;
        [SerializeField] private MonoBehaviour transitionGuard;
        private readonly HashSet<Collider> waitingPlayers = new HashSet<Collider>();
        private OpenDoorWithKey soundDoor;
        readonly List<Material> entranceMaterials = new List<Material>();
        Camera entranceCamera;
        float previousNearClip;

        public string DestinationScene => destinationScene;
        public string DestinationSpawnId => destinationSpawnId;
        public bool IsBlocked => transitionGuard is ISceneTransitionGuard guard && guard.IsTransitionBlocked;

        private void Reset() => GetComponent<BoxCollider>().isTrigger = true;

        private void Start()
        {
            if (gameObject.scene.name == "CampusBlock4" && destinationScene == "CampusExterior")
                BuildExitBacking();
            if (gameObject.scene.name == "Basement" && destinationScene == "CampusExterior" &&
                !GetComponent<PrisonCampusPortal>()) gameObject.AddComponent<PrisonCampusPortal>();
            if (gameObject.scene.name == "CampusBlock4" && destinationScene == "CampusExterior")
                foreach (var root in gameObject.scene.GetRootGameObjects())
                    foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                    {
                        if (renderer.name != "Saved entrance and technical facade") continue;
                        // This facade is approached from both sides. Keep the shared assets
                        // intact and make only this entrance's material instances two-sided.
                        var materials = renderer.sharedMaterials;
                        for (int i = 0; i < materials.Length; i++)
                        {
                            if (!materials[i]) continue;
                            var copy = new Material(materials[i]);
                            if (copy.HasProperty("_Cull")) copy.SetFloat("_Cull", 0);
                            if (copy.HasProperty("_CullMode")) copy.SetFloat("_CullMode", 0);
                            materials[i] = copy;
                            entranceMaterials.Add(copy);
                        }
                        renderer.sharedMaterials = materials;
                        renderer.forceRenderingOff = false;
                        renderer.allowOcclusionWhenDynamic = false;
                    }
            soundDoor = physicalDoor;
            if (soundDoor != null) return;
            // Some legacy portals omit physicalDoor. Associate audio with the nearby leaf
            // without changing that portal's existing key checks or passage rules.
            float nearest = 1.5f * 1.5f;
            Vector3 center = GetComponent<BoxCollider>().bounds.center;
            foreach (var candidate in FindObjectsByType<OpenDoorWithKey>())
            {
                float distance = candidate.SurfaceDistanceSquared(center);
                if (distance >= nearest) continue;
                nearest = distance;
                soundDoor = candidate;
            }
        }

        void BuildExitBacking()
        {
            // This interior is a separate scene. Keep a closed entrance behind the
            // transition volume so approaching the exit never reveals the scene void.
            Material surface = null;
            float nearest = float.PositiveInfinity;
            foreach (var root in gameObject.scene.GetRootGameObjects())
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                {
                    if (!renderer.name.ToLowerInvariant().Contains("door")) continue;
                    float distance = (renderer.bounds.center - transform.position).sqrMagnitude;
                    if (distance >= nearest || !renderer.sharedMaterial) continue;
                    surface = renderer.sharedMaterial;
                    nearest = distance;
                }
            var box = GetComponent<BoxCollider>();
            var backing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            backing.name = "Closed exterior entrance backing";
            backing.transform.SetParent(transform, false);
            backing.transform.localPosition = box.center - Vector3.forward * (box.size.z * .5f + .18f);
            backing.transform.localScale = new Vector3(box.size.x + .8f, box.size.y + 2.4f, .16f);
            // The trigger performs the transition; no invisible extra obstacle.
            var collision = backing.GetComponent<Collider>();
            collision.enabled = false;
            Destroy(collision);
            if (surface) backing.GetComponent<MeshRenderer>().sharedMaterial = surface;
            backing.GetComponent<MeshRenderer>().allowOcclusionWhenDynamic = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            // Arrivals during a scene load are deliberately not queued: they must leave first.
            if (UI.DoorReloadScreen.IsReloading) return;
            if (other.GetComponentInParent<PlayerMovement>() == null) return;
            waitingPlayers.Add(other);
            TryTransition(other);
        }

        private void OnTriggerStay(Collider other)
        {
            // A key can be collected or a physical door can unlock while already inside.
            if (waitingPlayers.Contains(other)) TryTransition(other);
        }

        private void OnTriggerExit(Collider other) => waitingPlayers.Remove(other);
        private void OnDisable() => waitingPlayers.Clear();

        private void LateUpdate()
        {
            if (gameObject.scene.name != "CampusBlock4" || destinationScene != "CampusExterior") return;
            var camera = Camera.main;
            if (!camera || camera.nearClipPlane <= .06f) return;
            if (!entranceCamera) { entranceCamera = camera; previousNearClip = camera.nearClipPlane; }
            camera.nearClipPlane = .06f;
        }

        private void OnDestroy()
        {
            if (entranceCamera && Mathf.Approximately(entranceCamera.nearClipPlane, .06f))
                entranceCamera.nearClipPlane = previousNearClip;
            foreach (var material in entranceMaterials) if (material) Destroy(material);
        }

        private void TryTransition(Collider other)
        {
            var player = other.GetComponentInParent<PlayerMovement>();
            if (player == null || UI.DoorReloadScreen.IsReloading || IsBlocked) return;
            var life = player.GetComponent<PlayerLife>();
            if (life != null && life.IsDead) return;
            var keys = player.GetComponent<PlayerPickupKey>();
            if ((requiresKey01 && (keys == null || !keys.key01)) ||
                (requiresKey02 && (keys == null || !keys.key02)) ||
                (requiresKey03 && (keys == null || !keys.key03))) return;
            // The player can cross a rotating door before its animation reaches 80%.
            // Waiting for IsOpen lets them leave this volume and fall outside the room.
            if (physicalDoor != null && !physicalDoor.IsUnlocked) return;
            if (waitForPhysicalDoorToOpen && physicalDoor != null && !physicalDoor.IsFullyOpen) return;

            if (UI.DoorReloadScreen.TryLoadScene(destinationScene, destinationSpawnId, destinationLabel))
            {
                // A physical leaf already plays when opening. Bare portals play on successful entry.
                if (soundDoor == null) GameplayAudio.Instance?.PlayDoorOpening();
                waitingPlayers.Clear();
            }
        }

        private void OnDrawGizmosSelected()
        {
            var box = GetComponent<BoxCollider>();
            if (box == null) return;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(0.75f, 0.15f, 0.08f, 0.35f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = new Color(1f, 0.55f, 0.15f);
            Gizmos.DrawWireCube(box.center, box.size);
        }
    }
}
