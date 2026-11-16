using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float _maxHealth = 100f;

    [SerializeField] bool _autoRespawn = false;
    [SerializeField] float _respawnDelay = 3f;

    void OnEnable() => OnDied += HandleDeath;
    void OnDisable() => OnDied -= HandleDeath;


    public float Current { get; private set; }
    public float Max => _maxHealth;
    public bool IsDead => Current <= 0f;
    public float Normalized => _maxHealth > 0f ? Current / _maxHealth : 0f;

    public event System.Action<float> OnDamaged;
    public event System.Action OnDied;

    void Awake() => Current = _maxHealth;

    public void TakeDamage(float amount, Collider hitCollider)
    {
        if (IsDead || amount <= 0f) return;

        Current = Mathf.Max(0f, Current - amount);
        Debug.Log($"[Health] {name} took {amount:F1} → {Current:F1}/{_maxHealth}");
        OnDamaged?.Invoke(amount);

        if (IsDead)
        {
            Debug.Log($"[Health] {name} DIED");
            OnDied?.Invoke();
        }
    }

    void HandleDeath()
    {
        if (!_autoRespawn) return;
        Invoke(nameof(ResetHealth), _respawnDelay);
    }


    public void Heal(float amount)
    {
        if (IsDead) return;
        Current = Mathf.Min(_maxHealth, Current + amount);
    }

    public void ResetHealth() => Current = _maxHealth;
}
