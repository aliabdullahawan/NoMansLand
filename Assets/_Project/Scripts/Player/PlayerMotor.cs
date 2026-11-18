using UnityEngine;
using UnityEngine.InputSystem;
using Mirror;

[RequireComponent(typeof(CharacterController))]
public class PlayerMotor : NetworkBehaviour
{
    [Header("Speeds (m/s)")]
    [SerializeField] float _walkSpeed = 4.2f;
    [SerializeField] float _sprintSpeed = 6.8f;
    [SerializeField] float _crouchSpeed = 2.2f;

    [Header("Jump & Gravity")]
    [SerializeField] float _jumpHeight = 1.0f;
    [SerializeField] float _gravity = -20f;

    [Header("Crouch")]
    [SerializeField] float _standHeight = 1.8f;
    [SerializeField] float _crouchHeight = 1.1f;
    [SerializeField] float _crouchLerpSpeed = 10f;
    [SerializeField] Transform _cameraPivot; 
    [SerializeField] float _standEyeHeight = 1.6f; 
    [SerializeField] float _crouchEyeHeight = 0.9f; 


    [Header("Ground Check")]
    [SerializeField] float _groundCheckDistance = 0.15f;
    [SerializeField] LayerMask _groundMask = ~0;

    CharacterController _cc;
    PlayerControls _controls;
    Vector2 _moveInput;
    Vector3 _velocity;
    bool _jumpQueued;
    bool _isGrounded;

    public bool IsSprinting { get; private set; }
    public bool IsDead { get; set; }
    public bool IsCrouching { get; private set; }
    public bool IsGrounded => _isGrounded;
    public float Speed01 { get; private set; }
    public Vector3 Velocity => _cc != null ? _cc.velocity : Vector3.zero;

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _controls = new PlayerControls();
    }

    void OnEnable()
    {
        // Intentionally empty, input setup is moved below
    }

    public override void OnStartLocalPlayer()
    {
        _controls.Player.Enable();
        _controls.Player.Jump.performed += OnJump;
        _controls.Player.Sprint.performed += OnSprintStart;
        _controls.Player.Sprint.canceled += OnSprintEnd;
        _controls.Player.Crouch.performed += OnCrouchStart;
        _controls.Player.Crouch.canceled += OnCrouchEnd;
    }


    void OnDisable()
    {
        if (!isLocalPlayer) return;
        _controls.Player.Disable();
        _controls.Player.Jump.performed -= OnJump;
        _controls.Player.Sprint.performed -= OnSprintStart;
        _controls.Player.Sprint.canceled -= OnSprintEnd;
        _controls.Player.Crouch.performed -= OnCrouchStart;
        _controls.Player.Crouch.canceled -= OnCrouchEnd;
    }

    void OnJump(InputAction.CallbackContext _) => _jumpQueued = true;
    void OnSprintStart(InputAction.CallbackContext _) => IsSprinting = true;
    void OnSprintEnd(InputAction.CallbackContext _) => IsSprinting = false;
    void OnCrouchStart(InputAction.CallbackContext _) => IsCrouching = true;
    void OnCrouchEnd(InputAction.CallbackContext _) => IsCrouching = false;

    void Update()
    {
        if (!isLocalPlayer) return;
        if (IsDead) return;
        _moveInput = _controls.Player.Move.ReadValue<Vector2>();

        _isGrounded = Physics.SphereCast(
            transform.position + Vector3.up * _cc.radius,
            _cc.radius * 0.95f,
            Vector3.down,
            out _,
            _cc.radius + _groundCheckDistance,
            _groundMask,
            QueryTriggerInteraction.Ignore);

        bool canSprint = IsSprinting && _moveInput.y > 0.1f && !IsCrouching;
        float targetSpeed = IsCrouching ? _crouchSpeed : canSprint ? _sprintSpeed : _walkSpeed;

        Vector3 wish = transform.right * _moveInput.x + transform.forward * _moveInput.y;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        if (_isGrounded && _velocity.y < 0f) _velocity.y = -2f;

        if (_isGrounded && _jumpQueued && !IsCrouching)
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        _jumpQueued = false;

        _velocity.y += _gravity * Time.deltaTime;

        Vector3 motion = wish * targetSpeed + Vector3.up * _velocity.y;
        _cc.Move(motion * Time.deltaTime);

        ApplyCrouchHeight();

        Vector3 flat = new Vector3(_cc.velocity.x, 0f, _cc.velocity.z);
        Speed01 = Mathf.Clamp01(flat.magnitude / _sprintSpeed);
    }

    void ApplyCrouchHeight()
    {
        float targetHeight = IsCrouching ? _crouchHeight : _standHeight;
        if (!Mathf.Approximately(_cc.height, targetHeight))
        {
            _cc.height = Mathf.Lerp(_cc.height, targetHeight, _crouchLerpSpeed * Time.deltaTime);
            _cc.center = Vector3.up * _cc.height * 0.5f;
        }

        if (_cameraPivot != null)
        {
            float targetEye = IsCrouching ? _crouchEyeHeight : _standEyeHeight;
            Vector3 p = _cameraPivot.localPosition;
            p.y = Mathf.Lerp(p.y, targetEye, _crouchLerpSpeed * Time.deltaTime);
            _cameraPivot.localPosition = p;
        }
    }

}
