using System.Collections.Generic;
using UnityEngine;

public class WavePlan
{
    private readonly List<ZombieSO> _types = new List<ZombieSO>();
    private readonly List<float> _weights = new List<float>();
    private float _totalWeight;

    public int WaveNumber { get; internal set; }
    public int HordeCount { get; internal set; }
    public float HealthMultiplier { get; internal set; } = 1f;
    public float DamageMultiplier { get; internal set; } = 1f;
    public int DamageBonus { get; internal set; }
    public bool IsBossWave { get; internal set; }
    public bool IsSpecialWave { get; internal set; }
    public ZombieSO BossType { get; internal set; }

    public int ZombieCount => HordeCount + (IsBossWave ? 1 : 0);
    public bool HasTypes => _types.Count > 0;

    internal void AddType(ZombieSO type, float weight)
    {
        if (type == null || weight <= 0f) return;

        _types.Add(type);
        _weights.Add(weight);
        _totalWeight += weight;
    }

    public ZombieSO PickType()
    {
        if (_types.Count == 0) return null;

        float roll = Random.Range(0f, _totalWeight);

        for (int i = 0; i < _types.Count; i++)
        {
            roll -= _weights[i];
            if (roll < 0f) return _types[i];
        }

        return _types[_types.Count - 1];
    }
}