using UnityEngine;

public enum WeaponClass { AR, SMG, LMG, SR, MR, Shotgun, Pistol, OldRifle }
public enum FireMode { Auto, Semi, Bolt }

[CreateAssetMenu(menuName = "NoMansLand/Weapon Data", fileName = "WD_NewWeapon")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    public string displayName = "New Weapon";
    public WeaponClass weaponClass = WeaponClass.AR;
    public FireMode fireMode = FireMode.Auto;

    [Header("Damage")]
    public float damage = 25f;
    public float headMultiplier = 2f;
    public float falloffStart = 30f;
    public float falloffEnd = 80f;
    [Range(0f, 1f)] public float minDamageScale = 0.6f;

    [Header("Fire")]
    public float rpm = 750f;
    public int magSize = 30;
    public int pellets = 1;
    public float reloadTime = 2.2f;

    [Header("Ballistics")]
    public bool projectile = false;
    public float muzzleVelocity = 800f;
    public float gravityScale = 1f;

    [Header("Accuracy (degrees)")]
    public float hipSpread = 2.5f;
    public float adsSpread = 0.2f;
    public float moveSpreadAdd = 1.5f;

    [Header("Recoil (x = yaw, y = pitch)")]
    public Vector2[] recoilPattern;
    public float recoilScale = 1f;
    public float adsRecoilScale = 0.7f;

    [Header("ADS")]
    public float adsFov = 55f;
    public float adsTime = 0.2f;

    [Header("Presentation")]
    public GameObject viewModel;
    public GameObject worldModel;
    public AudioClip[] fireClose;
    public AudioClip[] fireDistant;
    public AudioClip reload;

    public float SecondsPerShot => 60f / Mathf.Max(1f, rpm);
}
