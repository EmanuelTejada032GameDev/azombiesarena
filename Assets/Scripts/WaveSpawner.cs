using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public static WaveSpawner Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject _zombiePrefab;
    [SerializeField] private Transform[] _spawnEntrances;
    [SerializeField] private Transform _playerTarget;
    [SerializeField] private WaveProgressionSO _waveProgression;

    [Header("Wave Configuration")]
    [SerializeField] private float _timeBetweenSpawns = 1.5f;
    [SerializeField] private float _intermissionDuration = 5.0f;
    [SerializeField] private int _maxActiveZombiesCap = 24;
    [SerializeField] private bool _continuousMode = false;

    private int _currentWave = 0;
    private int _totalZombiesForCurrentWave;
    private int _zombiesSpawnedSoFar;
    private int _currentActiveZombiesCount;
    private bool _isIntermission = false;
    private WavePlan _currentPlan;

    public WavePlan CurrentPlan => _currentPlan;


    [Header("UI Data Streams")]
    [SerializeField] private IntSO _currentWaveVariable;
    [SerializeField] private IntSO _zombiesRemainingVariable;
    [SerializeField] private FloatSO _intermissionTimerVariable;


    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    private void OnEnable()
    {
        _currentWave = 0;
        StartNextWave();
    }


    private void StartNextWave()
    {
        if (_waveProgression == null)
        {
            Debug.LogError("[WaveSpawner] No WaveProgressionSO assigned.", this);
            return;
        }

        _currentWave++;
        _zombiesSpawnedSoFar = 0;
        if (!_continuousMode) _currentActiveZombiesCount = 0;

        _currentPlan = _waveProgression.BuildPlan(_currentWave);
        _totalZombiesForCurrentWave = _currentPlan.ZombieCount;

        if (_currentWaveVariable != null) _currentWaveVariable.Value = _currentWave;
        if (_zombiesRemainingVariable != null) _zombiesRemainingVariable.Value = _totalZombiesForCurrentWave;

        if (_intermissionTimerVariable != null) _intermissionTimerVariable.Value = 0f;

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        if (_totalZombiesForCurrentWave <= 0)
        {
            yield return null;
        }

        while (_zombiesSpawnedSoFar < _totalZombiesForCurrentWave)
        {
            if (_currentActiveZombiesCount < _maxActiveZombiesCap)
            {
                ZombieSO nextType = GetNextZombieType();

                if (nextType == null)
                {
                    Debug.LogWarning($"[WaveSpawner] No zombie type available on wave {_currentWave}. Ending spawns early.", this);
                    _totalZombiesForCurrentWave = _zombiesSpawnedSoFar;
                    break;
                }

                SpawnZombie(nextType);
                _zombiesSpawnedSoFar++;
                _currentActiveZombiesCount++;

                yield return new WaitForSeconds(_timeBetweenSpawns);
            }
            else
            {
                yield return null;
            }
        }

        if (!_continuousMode)
        {
            while (_currentActiveZombiesCount > 0)
            {
                yield return null;
            }
        }

        StartCoroutine(IntermissionRoutine());
    }

    private ZombieSO GetNextZombieType()
    {
        if (_currentPlan.IsBossWave && _zombiesSpawnedSoFar == 0)
        {
            return _currentPlan.BossType;
        }

        return _currentPlan.PickType();
    }

    private void SpawnZombie(ZombieSO zombieType)
    {
        int randomGateIndex = Random.Range(0, _spawnEntrances.Length);
        Transform chosenGate = _spawnEntrances[randomGateIndex];

        GameObject newZombie = Instantiate(_zombiePrefab, chosenGate.position, chosenGate.rotation);

        Zombie zombieScript = newZombie.GetComponent<Zombie>();

        if (zombieScript != null)
        {
            zombieScript.ApplyZombieSO(zombieType, _currentPlan.HealthMultiplier, _currentPlan.DamageMultiplier, _currentPlan.DamageBonus);
            zombieScript.InitializeTarget(_playerTarget);
        }

        HealthSystem zombieHealth = newZombie.GetComponent<HealthSystem>();
        if (zombieHealth != null)
        {
            zombieHealth.OnDied += HandleZombieDeath;
        }
    }

    private void HandleZombieDeath(object sender, System.EventArgs e)
    {
        HealthSystem deadZombieHealth = sender as HealthSystem;
        if (deadZombieHealth != null)
        {
            deadZombieHealth.OnDied -= HandleZombieDeath;
        }
        _currentActiveZombiesCount--;

        if (_zombiesRemainingVariable != null)
        {
            int totalLeftToKill = _totalZombiesForCurrentWave - (_zombiesSpawnedSoFar - _currentActiveZombiesCount);
            _zombiesRemainingVariable.Value = Mathf.Max(0, totalLeftToKill);
        }
    }

    private IEnumerator IntermissionRoutine()
    {
        _isIntermission = true;

        float timeRemaining = _continuousMode ? 0f : _intermissionDuration;

        while (timeRemaining > 0)
        {
            if (_intermissionTimerVariable != null)
            {
                _intermissionTimerVariable.Value = timeRemaining;
            }

            timeRemaining -= Time.deltaTime;
            yield return null;
        }


        if (_intermissionTimerVariable != null) _intermissionTimerVariable.Value = 0f;

        _isIntermission = false;
        StartNextWave();
    }
}