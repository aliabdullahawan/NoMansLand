using UnityEngine;
using TMPro;
using Mirror;

public class AmmoHUD : MonoBehaviour
{
    [SerializeField] TMP_Text _text;
    [SerializeField] WeaponController _weapons;

    void Update()
    {
        if (_weapons == null)
        {
            FindLocalPlayer();
            if (_weapons == null) return;
        }

        if (_text == null) return;
        _text.text = $"{_weapons.CurrentAmmo} / {_weapons.CurrentMagSize}";
    }

    void FindLocalPlayer()
    {
        foreach (var identity in FindObjectsByType<NetworkIdentity>(FindObjectsSortMode.None))
        {
            if (!identity.isLocalPlayer) continue;
            _weapons = identity.GetComponent<WeaponController>();
            return;
        }
    }
}
