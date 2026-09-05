using System;
using UnityEngine;

public class HealthSystem : MonoBehaviour, IDamagable
{
    public event EventHandler OnDamaged;
    public event EventHandler OnHealed;
    public event EventHandler OnDied;

    private int _healthAmount;
    [SerializeField] private int _maxHealthAmount;

    [SerializeField] private bool _useInvulnerability = false;
    private bool _isInvulnerable = false;

    [SerializeField] private float _invulnerabilityTimeFrame = 1.2f;
    private float _nextAllowedDamageTime = 0f;

    [SerializeField] private bool _destroyOnDeath = false;

    public void Awake()
    {
        if (_maxHealthAmount > 0)
            _healthAmount = _maxHealthAmount;
    }

    public void Initialize(int maxHealth)
    {
        _maxHealthAmount = maxHealth;
        _healthAmount = maxHealth;
    }

    public int HealthAmount => _healthAmount;
    public int MaxHealth => _maxHealthAmount;
    public float NormalizedHealthAmount => (float)_healthAmount / _maxHealthAmount;

    public void TakeDamage(int damageAmount)
    {

        if (_useInvulnerability && Time.time < _nextAllowedDamageTime)
        {
            return; 
        }

        if (_useInvulnerability)
        {
            _nextAllowedDamageTime = Time.time + _invulnerabilityTimeFrame;
        }

        _healthAmount -= damageAmount;
        _healthAmount = Mathf.Clamp(_healthAmount, 0, _maxHealthAmount);
        OnDamaged?.Invoke(this, EventArgs.Empty);

        if (_healthAmount <= 0)
            Die();
    }

    public void Heal(int amount)
    {
        _healthAmount += amount;
        _healthAmount = Mathf.Clamp(_healthAmount, 0, _maxHealthAmount);
        OnHealed?.Invoke(this, EventArgs.Empty);
    }

    public void HealFull()
    {
        if (IsFullHealth) return;

        _healthAmount = _maxHealthAmount;
        OnHealed?.Invoke(this, EventArgs.Empty);
    }

    public void Die()
    {
        if (!IsDead) TakeDamage(_healthAmount);
        OnDied?.Invoke(this, EventArgs.Empty);

        if(_destroyOnDeath)
            Destroy(gameObject);
    }

    public bool IsFullHealth => _healthAmount == _maxHealthAmount;
    public bool IsDead => _healthAmount <= 0;
    public int GetHealth() => _healthAmount;

    public void SetMaxHealthAmount(int maxHealthAmount, bool setHealthAmount = false)
    {
        _maxHealthAmount = maxHealthAmount;
        if (setHealthAmount) HealFull();
    }
}