using UnityEngine;
using System;

public abstract class Gun : MonoBehaviour
{
    [Header("Ammo")]

    // Quantidade máxima de munição que cabe no pente.
    [SerializeField] protected int magazineSize = 10;

    // Quantidade de munição que está atualmente no pente.
    [SerializeField] protected int currentAmmo = 10;

    // Quantidade de munição reserva, ou seja, fora do pente.
    [SerializeField] protected int reserveAmmo = 30;


    [Header("Reload")]

    // Referência ao componente responsável pelo processo/tempo de recarga.
    protected Recharging recharging;


    // Permitem que outros scripts consultem os valores das munições,
    // mas não consigam alterá-los diretamente.
    public int CurrentAmmo => currentAmmo;
    public int ReserveAmmo => reserveAmmo;
    public int MagazineSize => magazineSize;


    // É executado quando a arma é inicializada.
    protected virtual void Awake()
    {
        if (recharging == null)
        {
            recharging = GetComponent<Recharging>();
        }
    }


    // Tenta realizar um disparo.
    public event Action OnShoot;

    public void TryShoot()
    {
        if (recharging != null && recharging.IsReloading)
        {
            Debug.Log($"{name}: não pode atirar enquanto recarrega.");
            return;
        }

        if (!HasAmmo())
        {
            Debug.Log($"{name}: sem munição no pente.");
            return;
        }

        Shoot();
        ConsumeAmmo();

        OnShoot?.Invoke();
    }


    // Tenta iniciar a recarga da arma.
    public void TryReload()
    {
        // Sem o componente Recharging não conseguimos executar a recarga.
        if (recharging == null)
        {
            Debug.LogWarning($"{name}: componente Recharging não encontrado.");
            return;
        }

        // Se já estiver recarregando, não inicia outra recarga.
        if (recharging.IsReloading)
            return;

        // Se a munição atual já for igual ou maior que a capacidade do pente,
        // não existe necessidade de recarregar.
        if (currentAmmo >= magazineSize)
        {
            Debug.Log($"{name}: pente já está cheio.");
            return;
        }

        // Sem munição reserva não temos balas para colocar no pente.
        if (reserveAmmo <= 0)
        {
            Debug.Log($"{name}: sem munição reserva.");
            return;
        }

        // Inicia o processo de recarga.
        recharging.StartReload(this);
    }


    // Verifica se ainda existe munição no pente.
    protected bool HasAmmo()
    {
        return currentAmmo > 0;
    }


    // Consome uma unidade de munição do pente após um disparo.
    protected void ConsumeAmmo()
    {
        currentAmmo--;

        Debug.Log(
            $"{name}: {currentAmmo}/{magazineSize} | Reserva: {reserveAmmo}"
        );
    }


    // Adiciona munição à reserva da arma.
    public void AddAmmo(int amount)
    {
        // Impede adicionar valores inválidos ou negativos.
        if (amount <= 0)
            return;

        // Adiciona a quantidade recebida à munição reserva.
        reserveAmmo += amount;

        Debug.Log(
            $"{name}: pegou {amount} munições. Reserva: {reserveAmmo}"
        );
    }


    // Finaliza a recarga transferindo munição da reserva para o pente.
    public void FinishReload()
    {
        // Calcula quantas balas estão faltando para completar o pente.
        int missingAmmo = magazineSize - currentAmmo;

        // Decide quantas balas realmente podem ser colocadas.
        int ammoToReload = Mathf.Min(missingAmmo, reserveAmmo);

        // Adiciona as balas ao pente.
        currentAmmo += ammoToReload;

        // Remove da reserva a mesma quantidade colocada no pente.
        reserveAmmo -= ammoToReload;

        Debug.Log(
            $"{name}: recarga concluída. " +
            $"Pente: {currentAmmo}/{magazineSize} | Reserva: {reserveAmmo}"
        );
    }


    // Define que toda arma que herdar de Gun é obrigada
    // a implementar seu próprio comportamento de disparo.
    protected abstract void Shoot();
}