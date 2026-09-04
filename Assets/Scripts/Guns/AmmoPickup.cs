using UnityEngine;

public class AmmoPickup : MonoBehaviour
{
    // Quantidade de munição que será entregue ao jogador.
    [SerializeField] private int ammoAmount = 10;

    // Referência ao controlador que sabe qual arma está sendo usada atualmente.
    [SerializeField] private GunInputExample gunController;


    // É chamado quando algum Collider entra no Trigger deste objeto.
    private void OnTriggerEnter(Collider other)
    {
        // Verifica se quem entrou no Trigger foi o Player.
        if (!other.CompareTag("Player"))
            return;


        // Verifica se o GunController foi configurado.
        if (gunController == null)
        {
            Debug.LogWarning("AmmoPickup: GunController não foi configurado.");
            return;
        }


        // Pega a arma que o jogador está usando atualmente.
        Gun currentGun = gunController.CurrentGun;


        // Verifica se existe uma arma atual.
        if (currentGun == null)
        {
            Debug.LogWarning("AmmoPickup: nenhuma arma está selecionada.");
            return;
        }


        // Adiciona a munição na reserva da arma atual.
        currentGun.AddAmmo(ammoAmount);


        // Remove a caixa de munição da cena após ser coletada.
        Destroy(gameObject);
    }
}