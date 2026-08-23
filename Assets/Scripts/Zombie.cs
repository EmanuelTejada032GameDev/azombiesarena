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
    [SerializeField] private float _pollingInterval = 0.2f;

    private IDamagable _playerDamageable;
    private bool _canAttack = true;

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
        }
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

        Debug.Log($"Doing {_attackDamage} to player");
        _playerDamageable.TakeDamage(_attackDamage);

        // Visual indicator for graybox: Print to console or flash a color later
        Debug.Log($"{gameObject.name} bit the player!");

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

        // Trigger zombie death logic and FXs here
        Destroy(gameObject, .4f);
    }

    private void OnDisable()
    {
        if (_healthSystem != null) _healthSystem.OnDied -= HandleDeath;
        if (_trackingCoroutine != null) StopCoroutine(_trackingCoroutine);
    }
}
