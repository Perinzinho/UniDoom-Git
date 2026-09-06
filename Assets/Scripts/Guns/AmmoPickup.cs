using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private int ammoAmount = 10;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"Trigger detectado com: {other.name}, tag: {other.tag}");
        if (!other.CompareTag("Player"))
            return;

        // Busca o Gun em qualquer lugar do objeto que entrou no trigger (ou seus filhos).
        Gun currentGun = other.GetComponentInChildren<Gun>();

        if (currentGun == null)
        {
            Debug.LogWarning("AmmoPickup: nenhuma arma encontrada no Player.");
            return;
        }

        currentGun.AddAmmo(ammoAmount);
        Destroy(gameObject);
    }
}