using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UniDoom.Utils
{
    /// <summary>Player-root pose, with a unique ID inside its destination scene.</summary>
    [DisallowMultipleComponent]
    public sealed class SceneArrivalPoint : MonoBehaviour
    {
        [SerializeField] private string spawnId;
        public string SpawnId => spawnId;

        public static bool TryPlacePlayer(Scene scene, string id)
        {
            var player = GameManager.SessionPlayer;
            if (player == null) player = FindAnyObjectByType<PlayerMovement>();
            if (player == null) return false;

            foreach (var root in scene.GetRootGameObjects())
                foreach (var point in root.GetComponentsInChildren<SceneArrivalPoint>())
                    if (point.spawnId == id)
                    {
                        player.Teleport(point.transform.position, point.transform.rotation);
                        foreach (var aim in player.GetComponentsInChildren<CinemachinePanTilt>(true))
                        {
                            aim.ReferenceFrame = CinemachinePanTilt.ReferenceFrames.World;
                            aim.PanAxis.Value = point.transform.eulerAngles.y;
                            aim.TiltAxis.Value = 0f;
                        }
                        foreach (var camera in player.GetComponentsInChildren<CinemachineCamera>(true))
                            camera.PreviousStateIsValid = false;
                        Physics.SyncTransforms();
                        return true;
                    }
            // A misconfigured ID must not strand a persistent player at the previous scene's coordinates.
            foreach (var root in scene.GetRootGameObjects())
                if (root.name == "SpawnPoint")
                    player.Teleport(root.transform.position, root.transform.rotation);
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
            Gizmos.DrawRay(transform.position, transform.forward * 1.5f);
        }
    }
}
