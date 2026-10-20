using UnityEngine;

namespace TotalChaosMaps
{
    public enum TCMarkerType { PlayerStart, EnemySpawn, ItemSpawn, Exit }

    /// <summary>
    /// Ponto lógico gerado pelo TCMapBuilder. O seu gameplay (spawner de inimigos, loot,
    /// fim de fase) deve procurar estes marcadores com FindObjectsByType&lt;TCMapMarker&gt;().
    /// </summary>
    public class TCMapMarker : MonoBehaviour
    {
        public TCMarkerType type;

        [Tooltip("Rótulo livre: tipo de inimigo, id do item, 'key_1' para chaves, etc.")]
        public string label = "";

        void OnDrawGizmos()
        {
            switch (type)
            {
                case TCMarkerType.PlayerStart: Gizmos.color = new Color(0.3f, 0.9f, 0.4f); break;
                case TCMarkerType.EnemySpawn: Gizmos.color = new Color(0.9f, 0.2f, 0.2f); break;
                case TCMarkerType.ItemSpawn: Gizmos.color = new Color(0.95f, 0.8f, 0.2f); break;
                default: Gizmos.color = new Color(0.8f, 0.3f, 0.8f); break;
            }
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.35f);
            Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 0.9f);
        }
    }
}
