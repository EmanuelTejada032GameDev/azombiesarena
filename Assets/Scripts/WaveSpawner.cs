using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
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

    private void Start()
    {
        
    }


    private void OnEnable()
    {
        _currentWave = 0;
        StartNextWave();
    }

    private void StartNextWave()
    {
        _currentWave++;
        _zombiesSpawnedSoFar = 0;
        _currentActiveZombiesCount = 0;

        _totalZombiesForCurrentWave = _baseZombieCount + (_currentWave * _zombiesPerWaveMultiplier);

        //Debug.Log($"Wave {_currentWave} Started! Total Zombies: {_totalZombiesForCurrentWave}");

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
    }

    private IEnumerator IntermissionRoutine()
    {
        _isIntermission = true;
        //Debug.Log($"Wave Clear! Intermission started for {_intermissionDuration} seconds...");

        yield return new WaitForSeconds(_intermissionDuration);

        _isIntermission = false;
        StartNextWave();
    }
}
