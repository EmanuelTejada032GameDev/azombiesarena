using UnityEngine;

[CreateAssetMenu(fileName = "ZombieSO", menuName = "Zombies/Zombie Type")]
public class ZombieSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string _displayName;

    [Header("Stats")]
    [SerializeField] private int _maxHealth = 20;
    [SerializeField] private float _moveSpeed = 3.5f;

    [Header("Attack")]
    [SerializeField] private int _attackDamage = 1;
    [SerializeField] private float _attackCooldown = 1.0f;
    [SerializeField] private float _attackRange = 1.5f;

    [Header("Economy")]
    [SerializeField] private int _pointsPerHit = 10;
    [SerializeField] private int _pointsOnDeath = 60;
    [SerializeField] private int _xpReward = 20;

    [Header("Visuals")]
    [SerializeField] private Material _material;
    [SerializeField] private float _scaleMultiplier = 1f;

    public string DisplayName => _displayName;
    public int MaxHealth => _maxHealth;
    public float MoveSpeed => _moveSpeed;
    public int AttackDamage => _attackDamage;
    public float AttackCooldown => _attackCooldown;
    public float AttackRange => _attackRange;
    public int PointsPerHit => _pointsPerHit;
    public int PointsOnDeath => _pointsOnDeath;
    public Material Material => _material;
    public float ScaleMultiplier => _scaleMultiplier;


    public int XpReward => _xpReward;
}