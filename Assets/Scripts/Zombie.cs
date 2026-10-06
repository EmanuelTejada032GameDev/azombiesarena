using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour
{
    [Header("Navigation Settings")]
    [SerializeField] private float _pathUpdateInterval = 0.2f;

    private NavMeshAgent _agent;
    private Transform _targetPlayer;
    private Coroutine _trackingCoroutine;

    private HealthSystem _healthSystem;
    private Collider _collider;

    [Header("Attack Configuration")]
    [SerializeField] private float _attackRange = 1.5f;
    [SerializeField] private float _attackCooldown = 1.0f;
    [SerializeField] private int _attackDamage = 1;

    [Tooltip("Cooldown to check if player still in attack range")]
    [SerializeField] private float _pollingInterval = 0.2f;

    private IDamagable _playerDamageable;
    private bool _canAttack = true;

    [Header("Economy Rewards")]
    [SerializeField] private int _pointsPerHit = 10;
    [SerializeField] private int _pointsOnDeath = 60;
    [SerializeField] private int _xpOnDeath = 20;


    [Header("Animation")]
    [SerializeField] private float _speedDampTime = 0.1f;

    private Animator _animator;
    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    [Header("Attack Animation")]
    [SerializeField] private float _attackConeAngle = 90f;
    [SerializeField] private float _attackTurnSpeed = 180f;
    [SerializeField] private float _attackTimeout = 3f;

    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private bool _isAttacking;
    private bool _isDead;
    private Coroutine _attackTimeoutCoroutine;

    private static readonly int DeathHash = Animator.StringToHash("Death");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private bool _wasHitRecently = false;


    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _healthSystem = GetComponent<HealthSystem>();
        _animator = GetComponentInChildren<Animator>();
        _collider = GetComponentInChildren<Collider>();
    }

    private void Start()
    {
        StartCoroutine(AttackCheckRoutine());
    }

    private void Update()
    {
        if (_isAttacking && _targetPlayer != null)
        {
            Vector3 direction = _targetPlayer.position - transform.position;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, _attackTurnSpeed * Time.deltaTime);
            }
        }

        if (_animator == null || _agent == null || !_agent.enabled) return;

        _animator.SetFloat(SpeedHash, _agent.velocity.magnitude, _speedDampTime, Time.deltaTime);
    }

    private void OnEnable()
    {
        if (_healthSystem != null)
        {
            _healthSystem.OnDied += HandleDeath;
            _healthSystem.OnDamaged += HandleDamaged;
        }
    }


    public void ApplyZombieSO(ZombieSO zombieSO, float healthMultiplier = 1f, float damageMultiplier = 1f, int damageBonus = 0)
    {
        _agent.speed = zombieSO.MoveSpeed;

        int scaledHealth = Mathf.Max(1, Mathf.RoundToInt(zombieSO.MaxHealth * healthMultiplier));
        _healthSystem.Initialize(scaledHealth);

        transform.localScale = Vector3.one * zombieSO.ScaleMultiplier;

        _attackDamage = Mathf.Max(1, Mathf.RoundToInt(zombieSO.AttackDamage * damageMultiplier) + damageBonus);
        _attackCooldown = zombieSO.AttackCooldown;
        _attackRange = zombieSO.AttackRange;
        _pointsPerHit = zombieSO.PointsPerHit;
        _pointsOnDeath = zombieSO.PointsOnDeath;
        _xpOnDeath = zombieSO.XpReward;
    }

    private void HandleDamaged(object sender, EventArgs e)
    {
        EconomyManager.Instance.AddPoints(_pointsPerHit);

        if (_isDead || _isAttacking || _animator == null || _wasHitRecently) return;

        _wasHitRecently = true;
        _animator.SetTrigger(HitHash);
    }

    public void InitializeTarget(Transform playerTransform)
    {
        _targetPlayer = playerTransform;

        if (_targetPlayer != null)
        {
            _playerDamageable = _targetPlayer.GetComponent<IDamagable>();
            _trackingCoroutine = StartCoroutine(TrackTargetRoutine());
        }
    }

    private IEnumerator TrackTargetRoutine()
    {
        while (_targetPlayer != null)
        {
            _agent.SetDestination(_targetPlayer.position);
            yield return new WaitForSeconds(_pathUpdateInterval);
        }
    }

    private IEnumerator AttackCheckRoutine()
    {
        while (true)
        {

            if (!_isDead && _targetPlayer != null && _playerDamageable != null && _canAttack)
            {
                float distance = Vector3.Distance(transform.position, _targetPlayer.position);

                if (distance <= _attackRange)
                {
                    StartAttack();
                }
            }
            yield return new WaitForSeconds(_pollingInterval);
        }
    }

    private void StartAttack()
    {
        _canAttack = false;
        _isAttacking = true;

        if (_agent.enabled && _agent.isOnNavMesh) _agent.isStopped = true;

        if (_animator != null) _animator.SetTrigger(AttackHash);

        _attackTimeoutCoroutine = StartCoroutine(AttackTimeoutRoutine());
    }

    private IEnumerator AttackTimeoutRoutine()
    {
        yield return new WaitForSeconds(_attackTimeout);
        _attackTimeoutCoroutine = null;
        FinishAttack();
    }

    private IEnumerator CooldownRoutine()
    {
        yield return new WaitForSeconds(_attackCooldown);
        if (!_isDead) _canAttack = true;
    }

    private void FinishAttack()
    {
        if (!_isAttacking) return;

        _isAttacking = false;

        if (_attackTimeoutCoroutine != null)
        {
            StopCoroutine(_attackTimeoutCoroutine);
            _attackTimeoutCoroutine = null;
        }

        if (_agent != null && _agent.enabled && _agent.isOnNavMesh) _agent.isStopped = false;

        if (!_isDead) StartCoroutine(CooldownRoutine());
    }

    public void OnAttackHit()
    {
        if (!_isAttacking || _isDead || _targetPlayer == null || _playerDamageable == null) return;

        Vector3 toPlayer = _targetPlayer.position - transform.position;
        toPlayer.y = 0f;

        if (toPlayer.magnitude > _attackRange) return;
        if (Vector3.Angle(transform.forward, toPlayer) > _attackConeAngle * 0.5f) return;

        _playerDamageable.TakeDamage(_attackDamage);
    }

    public void OnAttackEnd()
    {
        FinishAttack();
    }

    public void OnHitReactEnd()
    {
        _wasHitRecently = false;
    }

    private void HandleDeath(object sender, EventArgs e)
    {
        _healthSystem.OnDied -= HandleDeath;
        _isDead = true;
        _isAttacking = false;
        _collider.enabled = false;

        _animator.SetTrigger(DeathHash);

        if (_attackTimeoutCoroutine != null)
        {
            StopCoroutine(_attackTimeoutCoroutine);
            _attackTimeoutCoroutine = null;
        }

        if (_trackingCoroutine != null)
        {
            StopCoroutine(_trackingCoroutine);
        }

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.enabled = false; 
        }

        EconomyManager.Instance.AddPoints(Mathf.RoundToInt(PlayerStats.Get(PlayerStats.StatType.CoinsPerKill, _pointsOnDeath)));
        XPManager.Instance.AddXP(Mathf.RoundToInt(PlayerStats.Get(PlayerStats.StatType.XpPerKill, _xpOnDeath)));

        // Trigger zombie death logic and FXs here
        Destroy(gameObject, 10f);
    }

    private void OnDisable()
    {
        if (_healthSystem != null)
        {
            _healthSystem.OnDied -= HandleDeath;
            _healthSystem.OnDamaged -= HandleDamaged;
        }

        if (_trackingCoroutine != null) StopCoroutine(_trackingCoroutine);
    }
}
