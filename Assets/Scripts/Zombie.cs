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

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _healthSystem = GetComponent<HealthSystem>();
    }

    private void Start()
    {
        StartCoroutine(AttackCheckRoutine());
    }

    private void OnEnable()
    {
        if (_healthSystem != null)
        {
            _healthSystem.OnDied += HandleDeath;
            _healthSystem.OnDamaged += HandleDamaged;
        }
    }

    private void HandleDamaged(object sender, EventArgs e)
    {
        EconomyManager.Instance.AddPoints(_pointsPerHit);
        // Damage Logic or visual effects
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
            if (_targetPlayer != null && _playerDamageable != null && _canAttack)
            {
                float distance = Vector3.Distance(transform.position, _targetPlayer.position);

                if (distance <= _attackRange)
                {
                    StartCoroutine(PerformAttackRoutine());
                }
            }
            yield return new WaitForSeconds(_pollingInterval);
        }
    }

    private IEnumerator PerformAttackRoutine()
    {
        _canAttack = false;

        _playerDamageable.TakeDamage(_attackDamage);

        // Visual indicator for graybox: Print to console or flash a color later

        yield return new WaitForSeconds(_attackCooldown);
        _canAttack = true;
    }

    private void HandleDeath(object sender, EventArgs e)
    {
        _healthSystem.OnDied -= HandleDeath;

        if (_trackingCoroutine != null)
        {
            StopCoroutine(_trackingCoroutine);
        }

        if (_agent != null && _agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.enabled = false; 
        }

        EconomyManager.Instance.AddPoints(_pointsOnDeath);
        // Trigger zombie death logic and FXs here
        Destroy(gameObject, .4f);
    }

    private void OnDisable()
    {
        if (_healthSystem != null) _healthSystem.OnDied -= HandleDeath;
        if (_trackingCoroutine != null) StopCoroutine(_trackingCoroutine);
    }
}
