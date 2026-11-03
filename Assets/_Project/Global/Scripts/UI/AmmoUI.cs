using TMPro;
using UnityEngine;

public class AmmoUI : MonoBehaviour
{
    [SerializeField] private TMP_Text ammoText;

    private Gun gun;

    public void SetGun(Gun newGun)
    {
        // Desinscreve da arma antiga (se houver)
        if (gun != null)
            gun.OnAmmoChanged -= UpdateText;

        gun = newGun;

        // Inscreve na nova
        if (gun != null)
        {
            gun.OnAmmoChanged += UpdateText;
        }
        UpdateText();
    }

    private void OnDestroy()
    {
        if (gun != null)
            gun.OnAmmoChanged -= UpdateText;
    }

    private void UpdateText()
    {
        if (ammoText != null)
            ammoText.text = gun != null ? $"{gun.CurrentAmmo}/{gun.ReserveAmmo}" : "0/0";
    }
}
