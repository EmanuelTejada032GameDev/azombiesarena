using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ZombieDropDefinition
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private int _amount = 1;
    [SerializeField, Range(0f, 1f)] private float _dropChance = 1f;
    [SerializeField] private bool _applyCoinsPerKillModifier;

    public GameObject Prefab => _prefab;
    public int Amount => _amount;
    public float DropChance => _dropChance;
    public bool ApplyCoinsPerKillModifier => _applyCoinsPerKillModifier;
}

[CreateAssetMenu(fileName = "ZombieSO", menuName = "Zombies/Zombie Type")]
public class ZombieSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _displayName;

    [Header("Stats")]
    [SerializeField] private int _maxHealth = 20;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveSpeed = 3.5f;
    [Tooltip("Fractional speed increase per wave after wave 1. For example, 0.03 means +3% per wave.")]
    [SerializeField, Min(0f)] private float _moveSpeedPercentPerWave;
    [Tooltip("Absolute speed limit for wave scaling. The shared zombie locomotion blend tree tops out at speed 4.")]
    [SerializeField, Min(0f)] private float _maxMoveSpeed = 4f;

    [Header("Attack")]
    [SerializeField] private int _attackDamage = 1;
    [SerializeField] private float _attackCooldown = 1.0f;
    [SerializeField] private float _attackRange = 1.5f;

    [Header("Rewards")]
    [SerializeField] private int _xpReward = 20;
    [SerializeField] private List<ZombieDropDefinition> _drops = new List<ZombieDropDefinition>();

    [Header("Visuals")]
    [SerializeField] private GameObject _prefab;
    [SerializeField] private float _scaleMultiplier = 1f;

    public string DisplayName => _displayName;
    public int MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public float MoveSpeedPercentPerWave => _moveSpeedPercentPerWave;
    public float MaxMoveSpeed => _maxMoveSpeed;
    public int AttackDamage => _attackDamage;
    public float AttackCooldown => _attackCooldown;
    public float AttackRange => _attackRange;
    public IReadOnlyList<ZombieDropDefinition> Drops => _drops;
    public GameObject Prefab => _prefab;
    public float ScaleMultiplier => _scaleMultiplier;
    public int XpReward => _xpReward;
}
