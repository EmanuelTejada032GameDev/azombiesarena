using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

[CreateAssetMenu(fileName = "WaveProgressionSO", menuName = "Zombies/Wave Progression")]
public class WaveProgressionSO : ScriptableObject
{
    public enum DamageScalingMode { Percent, Stepped }

    [Serializable]
    public class ZombieTypeEntry
    {
        [SerializeField] private ZombieSO _zombieType;
        [SerializeField, Min(1)] private int _unlockWave = 1;
        [SerializeField, Min(0f)] private float _startWeight = 1f;
        [SerializeField, Min(0f)] private float _weightPerWave = 0f;
        [SerializeField, Min(0f)] private float _maxWeight = 10f;
        [SerializeField] private bool _canBeSpecialWave = true;

        public ZombieSO ZombieType => _zombieType;
        public int UnlockWave => _unlockWave;
        public float StartWeight => _startWeight;
        public float WeightPerWave => _weightPerWave;
        public float MaxWeight => _maxWeight;
        public bool CanBeSpecialWave => _canBeSpecialWave;

        public bool IsUnlocked(int wave) => _zombieType != null && wave >= _unlockWave;

        public float GetWeight(int wave)
        {
            if (!IsUnlocked(wave)) return 0f;
            return Mathf.Min(_startWeight + _weightPerWave * (wave - _unlockWave), _maxWeight);
        }
    }

    [Header("Wave Size")]
    [SerializeField, Min(0)] private int _baseZombieCount = 5;
    [SerializeField, Min(0)] private int _zombiesPerWave = 3;

    [Header("Zombie Types")]
    [SerializeField] private ZombieTypeEntry[] _zombieTypes;

    [Header("Special Waves")]
    [SerializeField] private bool _enableSpecialWaves = true;
    [SerializeField, Min(1)] private int _specialWaveInterval = 7;

    [Header("Boss")]
    [SerializeField] private bool _enableBoss = true;
    [SerializeField] private ZombieSO _bossType;
    [SerializeField, Min(1)] private int _bossWaveInterval = 5;
    [SerializeField] private bool _bossAlone = false;
    [Tooltip("Fraction of the normal wave count that spawns alongside the boss when Boss Alone is off.")]
    [SerializeField, Range(0f, 1f)] private float _bossHordeFraction = 0.5f;

    [Header("Health Scaling")]
    [Tooltip("Fraction form: 0.1 = +10% max health per wave.")]
    [SerializeField, Min(0f)] private float _healthPercentPerWave = 0.1f;
    [SerializeField, Min(1f)] private float _maxHealthMultiplier = 5f;

    [Header("Damage Scaling")]
    [SerializeField] private DamageScalingMode _damageScalingMode = DamageScalingMode.Percent;

    [Header("Damage Scaling - Percent")]
    [Tooltip("Fraction form: 0.05 = +5% damage per wave.")]
    [SerializeField, Min(0f)] private float _damagePercentPerWave = 0.05f;
    [SerializeField, Min(1f)] private float _maxDamageMultiplier = 3f;

    [Header("Damage Scaling - Stepped")]
    [SerializeField, Min(1)] private int _wavesPerDamageStep = 5;
    [SerializeField, Min(0)] private int _maxDamageBonus = 3;

    public int BaseZombieCount => _baseZombieCount;
    public int ZombiesPerWave => _zombiesPerWave;
    public ZombieTypeEntry[] ZombieTypes => _zombieTypes;

    public bool EnableSpecialWaves => _enableSpecialWaves;
    public int SpecialWaveInterval => _specialWaveInterval;

    public bool EnableBoss => _enableBoss;
    public ZombieSO BossType => _bossType;
    public int BossWaveInterval => _bossWaveInterval;
    public bool BossAlone => _bossAlone;
    public float BossHordeFraction => _bossHordeFraction;

    public float HealthPercentPerWave => _healthPercentPerWave;
    public float MaxHealthMultiplier => _maxHealthMultiplier;

    public DamageScalingMode DamageMode => _damageScalingMode;
    public float DamagePercentPerWave => _damagePercentPerWave;
    public float MaxDamageMultiplier => _maxDamageMultiplier;
    public int WavesPerDamageStep => _wavesPerDamageStep;
    public int MaxDamageBonus => _maxDamageBonus;

    public WavePlan BuildPlan(int wave)
    {
        int normalCount = _baseZombieCount + wave * _zombiesPerWave;

        WavePlan plan = new WavePlan
        {
            WaveNumber = wave,
            HealthMultiplier = GetHealthMultiplier(wave)
        };

        ApplyDamageScaling(plan, wave);

        if (IsBossWave(wave))
        {
            plan.IsBossWave = true;
            plan.BossType = _bossType;

            if (!_bossAlone)
            {
                AddUnlockedTypes(plan, wave);
                plan.HordeCount = plan.HasTypes ? Mathf.RoundToInt(normalCount * _bossHordeFraction) : 0;
            }

            return plan;
        }

        if (IsSpecialWaveNumber(wave))
        {
            ZombieSO specialType = PickSpecialType(wave);

            if (specialType != null)
            {
                plan.IsSpecialWave = true;
                plan.AddType(specialType, 1f);
                plan.HordeCount = normalCount;
                return plan;
            }
        }

        AddUnlockedTypes(plan, wave);
        plan.HordeCount = plan.HasTypes ? normalCount : 0;

        if (!plan.HasTypes)
            Debug.LogWarning($"[WaveProgressionSO] No zombie types unlocked for wave {wave}.", this);

        return plan;
    }

    private bool IsBossWave(int wave)
    {
        return _enableBoss && _bossType != null && wave % _bossWaveInterval == 0;
    }

    private bool IsSpecialWaveNumber(int wave)
    {
        return _enableSpecialWaves && wave % _specialWaveInterval == 0;
    }

    private float GetHealthMultiplier(int wave)
    {
        return Mathf.Min(1f + _healthPercentPerWave * (wave - 1), _maxHealthMultiplier);
    }

    private void ApplyDamageScaling(WavePlan plan, int wave)
    {
        switch (_damageScalingMode)
        {
            case DamageScalingMode.Percent:
                plan.DamageMultiplier = Mathf.Min(1f + _damagePercentPerWave * (wave - 1), _maxDamageMultiplier);
                plan.DamageBonus = 0;
                break;

            case DamageScalingMode.Stepped:
                plan.DamageMultiplier = 1f;
                plan.DamageBonus = Mathf.Min((wave - 1) / _wavesPerDamageStep, _maxDamageBonus);
                break;
        }
    }

    private void AddUnlockedTypes(WavePlan plan, int wave)
    {
        if (_zombieTypes == null) return;

        foreach (ZombieTypeEntry entry in _zombieTypes)
        {
            if (entry == null || !entry.IsUnlocked(wave)) continue;
            plan.AddType(entry.ZombieType, entry.GetWeight(wave));
        }
    }

    private ZombieSO PickSpecialType(int wave)
    {
        if (_zombieTypes == null) return null;

        List<ZombieSO> eligible = new List<ZombieSO>();

        foreach (ZombieTypeEntry entry in _zombieTypes)
        {
            if (entry != null && entry.CanBeSpecialWave && entry.IsUnlocked(wave))
                eligible.Add(entry.ZombieType);
        }

        if (eligible.Count == 0) return null;

        return eligible[Random.Range(0, eligible.Count)];
    }
}