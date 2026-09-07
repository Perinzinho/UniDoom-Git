using System.Collections;
using UnityEngine;

public class Recharging : MonoBehaviour
{
    [SerializeField] private float reloadTime = 2f; //tempo de recarga

    public bool IsReloading { get; private set; } //indica se a arma está recarregando se sim marca como true

    public void StartReload(Gun gun)  //função para iniciar recarga da arma, recebe como parâmetro a arma que será recarregada
    {
        if (gun == null) //verifica se a arma é nula, se sim retorna
            return;

        if (IsReloading) //impede iniciar uma recarga se a arma já estiver recarregando
            return;

        StartCoroutine(ReloadCoroutine(gun));  //inicia o processo de recarga
    }

    private IEnumerator ReloadCoroutine(Gun gun)  //controla o tempo de recarga
    {
        IsReloading = true;  // marca a arma como recarregando

        DebugUI.Log($"{gun.name}: recarregando...");

        yield return new WaitForSeconds(reloadTime);   //espera o tempo de recarga sem travar o jogo

        gun.FinishReload();  //avisa a arma que a recarga terminou e atualiza a munição

        IsReloading = false;  //marca a recarga como concluída
    }
}
