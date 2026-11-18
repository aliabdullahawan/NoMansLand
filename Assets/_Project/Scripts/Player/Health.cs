using UnityEngine;
using Mirror;

public class Health : NetworkBehaviour
{
    [SerializeField] float _maxHealth = 100f;
    [SerializeField] bool _autoRespawn = false;
    [SerializeField] float _respawnDelay = 3f;
    [SerializeField] GameObject _visual;

    [SyncVar(hook = nameof(OnHealthChanged))]
    public float Current = 100f;

    [SyncVar(hook = nameof(OnDeadChanged))]
    public bool IsDead;

    public float Max => _maxHealth;
    public float Normalized => _maxHealth > 0f ? Current / _maxHealth : 0f;

    public event System.Action<float> OnDamaged;
    public event System.Action OnDied;

    public override void OnStartServer()
    {
        Current = _maxHealth;
        IsDead = false;
    }

    [Server]
    public void ServerTakeDamage(float amount, NetworkIdentity attacker)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        Debug.Log($"[Health] {name} took {amount:F1} → {Current:F1}/{_maxHealth}");

        if (Current <= 0f)
        {
            IsDead = true;
            var motor = GetComponent<PlayerMotor>();
            if (motor != null) motor.IsDead = true;
            
            Debug.Log($"[Health] {name} DIED");

            if (_autoRespawn)
                Invoke(nameof(ServerRespawn), _respawnDelay);
        }
    }



    [Server]
    void ServerRespawn()
    {
        Current = _maxHealth;
        IsDead = false;
        var motor = GetComponent<PlayerMotor>();
        if (motor != null) motor.IsDead = false;
        
    }

    void OnHealthChanged(float oldValue, float newValue)
    {
        if (newValue < oldValue)
            OnDamaged?.Invoke(oldValue - newValue);

        if (newValue <= 0f && oldValue > 0f)
            OnDied?.Invoke();
    }

    void OnDeadChanged(bool oldValue, bool newValue)
    {
        if (_visual != null) _visual.SetActive(!newValue);
    }
}
