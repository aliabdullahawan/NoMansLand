using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    [Header("Loadout")]
    [SerializeField] WeaponData[] _loadout = new WeaponData[2];

    [Header("References")]
    [SerializeField] Transform _camPivot;
    [SerializeField] PlayerLook _look;
    [SerializeField] PlayerMotor _motor;
    [SerializeField] LayerMask _hitMask = ~0;

    [Header("ADS")]
    [SerializeField] Camera _camera;
    [SerializeField] float _defaultFov = 60f;

    int _current;
    int[] _ammo = new int[2];
    float _nextFireTime;
    float _reloadEndTime;
    int _shotIndex;
    bool _isAds;
    float _adsBlend;

    public WeaponData CurrentWeapon => _loadout[_current];
    public int CurrentAmmo => _ammo[_current];
    public int CurrentMagSize => CurrentWeapon != null ? CurrentWeapon.magSize : 0;
    public bool IsReloading => Time.time < _reloadEndTime;
    public bool IsAds => _isAds;

    void Start()
    {
        for (int i = 0; i < _loadout.Length; i++)
            _ammo[i] = _loadout[i] != null ? _loadout[i].magSize : 0;
    }

    void Update()
    {
        if (CurrentWeapon == null) return;

        HandleAds();
        HandleSwitch();
        HandleReload();
        HandleFire();
    }

    void HandleAds()
    {
        bool wantAds = Mouse.current != null
                       && Mouse.current.rightButton.isPressed
                       && !_motor.IsSprinting
                       && !IsReloading;

        _isAds = wantAds;
        float target = _isAds ? 1f : 0f;
        _adsBlend = Mathf.MoveTowards(_adsBlend, target, Time.deltaTime / Mathf.Max(0.01f, CurrentWeapon.adsTime));

        if (_camera != null)
            _camera.fieldOfView = Mathf.Lerp(_defaultFov, CurrentWeapon.adsFov, _adsBlend);
    }

    void HandleSwitch()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) SwitchTo(0);
        if (kb.digit2Key.wasPressedThisFrame) SwitchTo(1);
    }

    void HandleReload()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.rKey.wasPressedThisFrame) return;
        if (IsReloading || _ammo[_current] >= CurrentWeapon.magSize) return;

        _reloadEndTime = Time.time + CurrentWeapon.reloadTime;
        Invoke(nameof(FinishReload), CurrentWeapon.reloadTime);
    }

    void FinishReload()
    {
        if (CurrentWeapon != null) _ammo[_current] = CurrentWeapon.magSize;
    }

    void HandleFire()
    {
        var mouse = Mouse.current;
        if (mouse == null) return;

        bool wantFire = CurrentWeapon.fireMode == FireMode.Auto
            ? mouse.leftButton.isPressed
            : mouse.leftButton.wasPressedThisFrame;

        if (!mouse.leftButton.isPressed) _shotIndex = 0;

        if (!wantFire) return;
        if (Time.time < _nextFireTime) return;
        if (IsReloading) return;
        if (_motor.IsSprinting) return;
        if (_ammo[_current] <= 0) return;

        _nextFireTime = Time.time + CurrentWeapon.SecondsPerShot;
        Fire();
    }

    void Fire()
    {
        _ammo[_current]--;

        for (int i = 0; i < CurrentWeapon.pellets; i++)
        {
            Vector3 dir = ApplySpread(_camPivot.forward);
            if (CurrentWeapon.pellets > 1)
                dir = (dir + Random.insideUnitSphere * 0.06f).normalized;

            if (Physics.Raycast(_camPivot.position, dir, out RaycastHit hit, 600f,
                                _hitMask, QueryTriggerInteraction.Ignore))
            {
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                    health.TakeDamage(ComputeDamage(hit, dir), hit.collider);
            }
        }

        ApplyRecoil();
    }

    float ComputeDamage(RaycastHit hit, Vector3 dir)
    {
        float dist = Vector3.Distance(_camPivot.position, hit.point);
        float t = Mathf.InverseLerp(CurrentWeapon.falloffStart, CurrentWeapon.falloffEnd, dist);
        float dmg = CurrentWeapon.damage * Mathf.Lerp(1f, CurrentWeapon.minDamageScale, t);

        var hitbox = hit.collider.GetComponent<Hitbox>();
        if (hitbox != null)
            dmg *= hitbox.isHead ? CurrentWeapon.headMultiplier : hitbox.multiplier;

        return dmg;
    }

    Vector3 ApplySpread(Vector3 forward)
    {
        float spreadDeg = _isAds
            ? CurrentWeapon.adsSpread
            : CurrentWeapon.hipSpread + _motor.Speed01 * CurrentWeapon.moveSpreadAdd;

        float radius = Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
        Vector2 r = Random.insideUnitCircle * radius;

        return (forward + _camPivot.right * r.x + _camPivot.up * r.y).normalized;
    }

    void ApplyRecoil()
    {
        var pattern = CurrentWeapon.recoilPattern;
        if (pattern == null || pattern.Length == 0 || _look == null) return;

        Vector2 kick = pattern[Mathf.Min(_shotIndex, pattern.Length - 1)]
                     * (_isAds ? CurrentWeapon.adsRecoilScale : 1f)
                     * CurrentWeapon.recoilScale;

        _look.RecoilKick += kick;
        _shotIndex++;
    }

    void SwitchTo(int index)
    {
        if (index < 0 || index >= _loadout.Length) return;
        if (_loadout[index] == null || index == _current) return;

        CancelInvoke(nameof(FinishReload));
        _current = index;
        _shotIndex = 0;
        _nextFireTime = Time.time + 0.25f;
    }
}
