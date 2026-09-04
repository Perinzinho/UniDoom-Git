using UnityEngine;

public class GunInputExample : MonoBehaviour
{
    // Arma que o jogador está usando atualmente.
    [SerializeField] private Gun currentGun;

    public Gun CurrentGun => currentGun;


    // É executado a cada frame e verifica os comandos do jogador.
    private void Update()
    {
        if (currentGun == null)
            return;

        // Botão esquerdo do mouse tenta realizar um disparo.
        if (Input.GetMouseButtonDown(0))
        {
            currentGun.TryShoot();
        }

        // Tecla R tenta recarregar a arma atual.
        if (Input.GetKeyDown(KeyCode.R))
        {
            currentGun.TryReload();
        }
    }


    // Altera qual arma está sendo utilizada atualmente.
    public void ChangeGun(Gun newGun)
    {
        currentGun = newGun;
    }
}