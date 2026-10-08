using UnityEngine;

public class OpenDoorWithKey : MonoBehaviour
{
    [Header("Chaves necessárias para abrir esta porta")]
    [SerializeField] private bool requiresKey01;
    [SerializeField] private bool requiresKey02;
    [SerializeField] private bool requiresKey03;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        // Busca também no pai, caso o collider esteja em um filho
        var player = other.GetComponentInParent<PlayerPickupKey>();
        if (player == null) return;

        if ((requiresKey01 && player.key01) ||
            (requiresKey02 && player.key02) ||
            (requiresKey03 && player.key03))
        {
            OpenDoor();
        }
    }

    private void OpenDoor()
    {
        // Tocar animação aqui, se quiser
        Destroy(gameObject);
    }
}