using UnityEngine;

public enum WeaponFiringMode
{
    SemiAutomatic, 
    FullAutomatic, 
    Burst         
}

[CreateAssetMenu(fileName = "NewWeaponProfile", menuName = "Weapons/Weapon Config")]
public class WeaponDataConfig : ScriptableObject
{
    [Header("Weapon Identity")]
    [SerializeField] private string _weaponName;
    [SerializeField] private GameObject _weaponModelPrefab;
    [SerializeField] private GameObject _projectilePrefab;

    [Header("Combat Stat Attributes")]
    [SerializeField] private int _damage = 1;

    [Header("Firing Mechanics")]
    [SerializeField] private WeaponFiringMode _firingMode = WeaponFiringMode.SemiAutomatic;
    [SerializeField] private float _fireCooldown = 0.2f; // Generic Cooldown

    [Header("Burst Settings (Only if Firing Mode is Burst)")]
    [SerializeField] private int _bulletsPerBurst = 3;
    [SerializeField] private float _burstDelay = 0.05f; // Micro-delay between bullets inside a burst (M8-Pharo like guns)

    [Header("Ballistics Spread / Shotgun Attributes")]
    [Tooltip("Number of individual projectiles spawned at once. Keep at 1 for standard guns, increase for shotguns.")]
    [SerializeField] private int _pelletCount = 1;
    [Tooltip("Maximum angle of random trajectory deviation in degrees (0 means perfectly straight).")]
    [SerializeField] private float _spreadAngle = 0f;

    [Header("Handling & Kickback")]
    [Tooltip("Force metric to be read by perspective cameras or mesh displacement solvers.")]
    [SerializeField] private float _recoilForce = 1.0f;

    [SerializeField] private int _maxMagazineSize = 30;
    [SerializeField] private int _maxReserveAmmo = 90;
    [SerializeField] private float _reloadDuration = 2.0f;

    public string WeaponName => _weaponName;
    public GameObject WeaponModelPrefab => _weaponModelPrefab;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public int Damage => _damage;
    public WeaponFiringMode FiringMode => _firingMode;
    public float FireCooldown => _fireCooldown;
    public int BulletsPerBurst => _bulletsPerBurst;
    public float BurstDelay => _burstDelay;
    public int PelletCount => _pelletCount;
    public float SpreadAngle => _spreadAngle;
    public float RecoilForce => _recoilForce;
    public int MaxMagazineSize => _maxMagazineSize;
    public int MaxReserveAmmo => _maxReserveAmmo;
    public float ReloadDuration => _reloadDuration;

}