using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

public class WeaponController : NetworkBehaviour
{
    [Header("Loadout")]
    [SerializeField] WeaponData[] _loadout = new WeaponData[2];

    [Header("References")]
    [SerializeField] Transform _camPivot;
    [SerializeField] PlayerLook _look;
    [SerializeField] PlayerMotor _motor;
    [SerializeField] LayerMask _hitMask = ~0;
    [SerializeField] Camera _camera;
    [SerializeField] float _defaultFov = 60f;

    [Header("Feedback")]
    [SerializeField] GameObject _damagePopupPrefab;
    [SerializeField] float _popupHeight = 1.2f;
    [SerializeField] bool _showDamageNumbers = true;
    [SerializeField] bool _autoReload = true;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    int _current;

    [SyncVar] int _ammoPrimary;
    [SyncVar] int _ammoSecondary;

    float _nextFireTime;
    float _serverNextFireTime;
    float _reloadEndTime;

    int _shotIndex;
    bool _isAds;
    float _adsBlend;

    public WeaponData CurrentWeapon => _loadout[_current];
    public int CurrentAmmo => _current == 0 ? _ammoPrimary : _ammoSecondary;

    int GetAmmo(int index) => index == 0 ? _ammoPrimary : _ammoSecondary;

    [Server]
    void SetAmmo(int index, int value)
    {
        if (index == 0) _ammoPrimary = value;
        else _ammoSecondary = value;
    }


    public int CurrentMagSize => CurrentWeapon != null ? CurrentWeapon.magSize : 0;
    public bool IsReloading => Time.time < _reloadEndTime;
    public bool IsAds => _isAds;

    public override void OnStartServer()
    {
        _ammoPrimary = _loadout.Length > 0 && _loadout[0] != null ? _loadout[0].magSize : 0;
        _ammoSecondary = _loadout.Length > 1 && _loadout[1] != null ? _loadout[1].magSize : 0;
    }


    void Update()
    {
        if (!isLocalPlayer) return;
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
        _adsBlend = Mathf.MoveTowards(_adsBlend, target,
            Time.deltaTime / Mathf.Max(0.01f, CurrentWeapon.adsTime));

        if (_camera != null)
            _camera.fieldOfView = Mathf.Lerp(_defaultFov, CurrentWeapon.adsFov, _adsBlend);
    }

    void HandleSwitch()
    {
        var kb = Keyboard.current;
        if (kb == null) return;
        if (kb.digit1Key.wasPressedThisFrame) CmdSwitch(0);
        if (kb.digit2Key.wasPressedThisFrame) CmdSwitch(1);
    }

    void HandleReload()
    {
        var kb = Keyboard.current;
        if (kb == null || !kb.rKey.wasPressedThisFrame) return;
        CmdReload();
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
        if (CurrentAmmo <= 0)
        {
            if (_autoReload) CmdReload();
            return;
        }

        _nextFireTime = Time.time + CurrentWeapon.SecondsPerShot;

        Vector3 origin = _camPivot.position;
        Vector3 dir = ApplySpread(_camPivot.forward);
        ApplyRecoil();

        CmdFire(origin, dir);
    }

    [Command]
    void CmdFire(Vector3 origin, Vector3 dir)
    {
        if (CurrentWeapon == null) return;
        if (GetAmmo(_current) <= 0) return;
        if (Time.time < _serverNextFireTime) return;
        if (IsReloading) return;

        _serverNextFireTime = Time.time + CurrentWeapon.SecondsPerShot;
        SetAmmo(_current, GetAmmo(_current) - 1);

        for (int i = 0; i < CurrentWeapon.pellets; i++)
        {
            Vector3 pelletDir = CurrentWeapon.pellets > 1
                ? (dir + Random.insideUnitSphere * 0.06f).normalized
                : dir;

            if (Physics.Raycast(origin, pelletDir, out RaycastHit hit, 600f,
                                _hitMask, QueryTriggerInteraction.Ignore))
            {
                var health = hit.collider.GetComponentInParent<Health>();
                if (health != null)
                {
                    float dmg = ComputeDamage(hit, origin);
                    health.ServerTakeDamage(dmg, netIdentity);
                    SpawnPopup(hit.point, dmg, IsHeadshot(hit.collider));
                }
            }
        }

        RpcFireFX(origin, dir);
    }

    [Command]
    void CmdReload()
    {
        if (CurrentWeapon == null) return;
        if (IsReloading) return;
        if (GetAmmo(_current) >= CurrentWeapon.magSize) return;

        _reloadEndTime = Time.time + CurrentWeapon.reloadTime;
        Invoke(nameof(ServerFinishReload), CurrentWeapon.reloadTime);
    }

    [Server]
    void ServerFinishReload()
    {
        if (CurrentWeapon != null) SetAmmo(_current, CurrentWeapon.magSize);
    }

    [Command]
    void CmdSwitch(int index)
    {
        if (index < 0 || index >= _loadout.Length) return;
        if (_loadout[index] == null || index == _current) return;

        CancelInvoke(nameof(ServerFinishReload));
        _current = index;
        _shotIndex = 0;
        _nextFireTime = Time.time + 0.25f;
    }

    [ClientRpc]
    void RpcFireFX(Vector3 origin, Vector3 dir)
    {
        if (isLocalPlayer) return;
        // Phase 4: tracer, muzzle flash, gunshot audio for remote players
    }

    void OnWeaponChanged(int oldIndex, int newIndex)
    {
        _shotIndex = 0;
    }

    float ComputeDamage(RaycastHit hit, Vector3 origin)
    {
        float dist = Vector3.Distance(origin, hit.point);
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

    [Server]
    void SpawnPopup(Vector3 worldPos, float damage, bool headshot)
    {
        if (!_showDamageNumbers) return;
        if (_damagePopupPrefab == null) return;

        var go = Instantiate(_damagePopupPrefab, worldPos + Vector3.up * _popupHeight, Quaternion.identity);
        go.GetComponent<DamagePopup>()?.Show(damage, headshot);
    }

    static bool IsHeadshot(Collider col)
    {
        var hb = col.GetComponent<Hitbox>();
        return hb != null && hb.isHead;
    }

}
