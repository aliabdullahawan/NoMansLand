using UnityEngine;
using Mirror;
using UnityEngine.InputSystem;

public class PlayerLook : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] Transform _pivot;
    [SerializeField] Camera _camera;

    [Header("Sensitivity")]
    [SerializeField] float _sensitivity = 0.12f;
    [SerializeField] float _minPitch = -85f;
    [SerializeField] float _maxPitch = 85f;

    [Header("Recoil Recovery")]
    [SerializeField] float _recoilRecoverySpeed = 25f;

    float _pitch;
    PlayerControls _controls;

    public Vector2 RecoilKick;
    public float Sensitivity { get => _sensitivity; set => _sensitivity = value; }

    void Awake()
    {
        _controls = new PlayerControls();
    }

    void OnEnable()
    {
        _controls.Player.Enable();
        LockCursor(true);
        if (_camera != null) _camera.gameObject.SetActive(true);
    }

    void OnDisable()
    {
        _controls.Player.Disable();
        LockCursor(false);
    }

    void Update()
    {
        if (!isLocalPlayer) return;
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 mouseDelta = _controls.Player.Look.ReadValue<Vector2>() * _sensitivity;

        float yaw = mouseDelta.x + RecoilKick.x;
        float pitchDelta = mouseDelta.y + RecoilKick.y;

        RecoilKick = Vector2.Lerp(RecoilKick, Vector2.zero, _recoilRecoverySpeed * Time.deltaTime);

        _pitch = Mathf.Clamp(_pitch - pitchDelta, _minPitch, _maxPitch);
        _pivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        transform.Rotate(Vector3.up * yaw, Space.World);
    }

    public override void OnStartLocalPlayer()
    {
        LockCursor(true);
        if (_camera != null) _camera.gameObject.SetActive(true);
    }

    static void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
