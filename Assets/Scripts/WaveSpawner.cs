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

    [Header("Wave Configuration")]
    [SerializeField] private int _baseZombieCount = 5;
    [SerializeField] private int _zombiesPerWaveMultiplier = 3;
    [SerializeField] private float _timeBetweenSpawns = 1.5f;
    [SerializeField] private float _intermissionDuration = 5.0f;
    [SerializeField] private int _maxActiveZombiesCap = 24;

    private int _currentWave = 0;
    private int _totalZombiesForCurrentWave;
    private int _zombiesSpawnedSoFar;
    private int _currentActiveZombiesCount;
    private bool _isIntermission = false;


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
        //StartNextWave();
    }


    private void StartNextWave()
    {
        _currentWave++;
        _zombiesSpawnedSoFar = 0;
        _currentActiveZombiesCount = 0;

        _totalZombiesForCurrentWave = _baseZombieCount + (_currentWave * _zombiesPerWaveMultiplier);

        if (_currentWaveVariable != null) _currentWaveVariable.Value = _currentWave;
        if (_zombiesRemainingVariable != null) _zombiesRemainingVariable.Value = _totalZombiesForCurrentWave;

        if (_intermissionTimerVariable != null) _intermissionTimerVariable.Value = 0f;

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        while (_zombiesSpawnedSoFar < _totalZombiesForCurrentWave)
        {
            if (_currentActiveZombiesCount < _maxActiveZombiesCap)
            {
                SpawnZombie();
                _zombiesSpawnedSoFar++;
                _currentActiveZombiesCount++;

                yield return new WaitForSeconds(_timeBetweenSpawns);
            }
            else
            {
                yield return null;
            }
        }

        while (_currentActiveZombiesCount > 0)
        {
            yield return null;
        }

        StartCoroutine(IntermissionRoutine());
    }

    private void SpawnZombie()
    {
        int randomGateIndex = Random.Range(0, _spawnEntrances.Length);
        Transform chosenGate = _spawnEntrances[randomGateIndex];

        GameObject newZombie = Instantiate(_zombiePrefab, chosenGate.position, chosenGate.rotation);

        Zombie zombieScript = newZombie.GetComponent<Zombie>();

        if (zombieScript != null)
        {
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

        float timeRemaining = _intermissionDuration;

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
