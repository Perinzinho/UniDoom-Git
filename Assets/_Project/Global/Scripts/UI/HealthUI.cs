using TMPro;
using UnityEngine;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private TMP_Text healthText;

    private PlayerLife playerLife;

    public void SetPlayer(PlayerLife newPlayerLife)
    {
        // Desinscreve do player antigo (se houver)
        if (playerLife != null)
            playerLife.OnHealthChanged -= UpdateText;

        playerLife = newPlayerLife;

        // Inscreve no novo e mostra o valor atual
        if (playerLife != null)
        {
            playerLife.OnHealthChanged += UpdateText;
            UpdateText(playerLife.CurrentHealth, playerLife.MaxHealth);
        }
    }

    private void OnDestroy()
    {
        if (playerLife != null)
            playerLife.OnHealthChanged -= UpdateText;
    }

    private void UpdateText(int current, int max)
    {
        healthText.text = $"{current}/{max}";
    }
}