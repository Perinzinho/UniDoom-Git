using UnityEngine;

public class MedkitPickup : MonoBehaviour
{
    [Header("Heal Settings")]
    [SerializeField] private int healAmount = 50;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Trigger com: " + other.gameObject.name + " | Tag: " + other.tag);

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detectado!");

            PlayerLife playerLife = other.GetComponent<PlayerLife>();
            if (playerLife != null)
            {
                Debug.Log("PlayerLife encontrado! Vida atual: " + playerLife.CurrentHealth);

                if (playerLife.CurrentHealth < playerLife.MaxHealth)
                {
                    Debug.Log("Curando " + healAmount + " de vida!");
                    playerLife.Heal(healAmount);
                    Destroy(gameObject);
                }
                else
                {
                    Debug.Log("Vida cheia, medkit não coletado.");
                }
            }
            else
            {
                Debug.Log("PlayerLife NÃO encontrado no player!");
            }
        }
    }
}