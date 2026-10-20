using UnityEngine;

namespace UniDoom.Utils
{
    /// <summary>
    /// Trigger de área para passagem entre setores ou corredores.
    /// Exibe a tela de reload e compilação de shaders quando o jogador entra no volume do trigger.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class DoorReloadTrigger : MonoBehaviour
    {
        [Header("Configurações do Setor")]
        [Tooltip("Nome do setor ou sala exibido na tela de reload.")]
        [SerializeField] private string sectorName = "NOVO SETOR";

        [Tooltip("Tempo mínimo em segundos entre ativações deste trigger.")]
        [SerializeField] private float cooldown = 4f;

        [Tooltip("Se marcado, o trigger dispara apenas uma vez e depois é desativado.")]
        [SerializeField] private bool oneShot = false;

        private float lastTriggerTime = -999f;
        private bool hasTriggered = false;

        private void Awake()
        {
            var col = GetComponent<Collider>();
            if (col != null)
            {
                col.isTrigger = true;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (oneShot && hasTriggered) return;
            if (Time.time < lastTriggerTime + cooldown) return;

            // Verifica se é o jogador (PlayerMovement, PlayerPickupKey ou Tag Player)
            if (other.CompareTag("Player") ||
                other.GetComponentInParent<PlayerMovement>() != null ||
                other.GetComponentInParent<PlayerPickupKey>() != null)
            {
                hasTriggered = true;
                lastTriggerTime = Time.time;
                UI.DoorReloadScreen.TriggerReload(sectorName);
            }
        }
    }
}
