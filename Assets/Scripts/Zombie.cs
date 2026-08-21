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

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _healthSystem = GetComponent<HealthSystem>();
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
