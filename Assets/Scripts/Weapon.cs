using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectPooler _bulletPool;
    [SerializeField] private Transform _muzzle;

    [Header("Audio Components")]
    [SerializeField] private AudioSource _weaponAudioSource;

    private const float MinimumCursorAimDistance = 0.25f;

    private WeaponInstanceState _state;

    private float _nextFireTime;
    private bool _isBursting;
    private bool _isReloading;

    public WeaponDataConfig Config => _state?.BlueprintConfig;
    public WeaponInstanceState State => _state;
    public bool IsReloading => _isReloading;
    public Transform Muzzle => _muzzle;

    public int EffectiveMaxMagazineSize => Config == null
        ? 0
        : Mathf.RoundToInt(WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.MagSize, Config.Category, Config.MaxMagazineSize));

    private float EffectiveFireCooldown => WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.FireRate, Config.Category, Config.FireCooldown);
    private float EffectiveReloadDuration => WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.ReloadSpeed, Config.Category, Config.ReloadDuration);

    public EventHandler OnAmmoChanged;
    public static event Action<float> OnShoot;

    /// <summary>
    /// Injects the runtime data instance packet and ties this physical prefab shell to its unique stats.
    /// </summary>
    public void InitializeWeapon(WeaponInstanceState instanceState, ObjectPooler matchingPool)
    {
        _state = instanceState;
        _bulletPool = matchingPool;

        _nextFireTime = 0f;
        _isBursting = false;
        _isReloading = false;
    }

    public void ProcessFireRequest()
    {
        if (Time.time < _nextFireTime || _isBursting || _isReloading || _state == null || _state.CurrentMagazineAmmo <= 0) return;

        switch (Config.FiringMode)
        {
            case WeaponFiringMode.SemiAutomatic:
            case WeaponFiringMode.FullAutomatic:
                ExecuteFireCycle();
                _nextFireTime = Time.time + EffectiveFireCooldown;
                break;

            case WeaponFiringMode.Burst:
                StartCoroutine(ExecuteBurstRoutine());
                _nextFireTime = Time.time + EffectiveFireCooldown;
                break;
        }
    }

    public void ProcessReloadRequest()
    {
        if (_isReloading || _state == null || _state.CurrentMagazineAmmo >= EffectiveMaxMagazineSize || _state.CurrentReserveAmmo <= 0) return;

        StartCoroutine(ExecuteReloadRoutine());
    }

    private void ExecuteFireCycle()
    {
        if (_bulletPool == null || _state.CurrentMagazineAmmo <= 0) return;


        _state.CurrentMagazineAmmo--;
        OnShoot?.Invoke(Config.RecoilForce);

        Config.ShootEvent.Play(_weaponAudioSource);

        for (int i = 0; i < Config.PelletCount; i++)
        {
            ExecuteSingleShot();
        }

        OnAmmoChanged?.Invoke(this, EventArgs.Empty);

        if (_state.CurrentMagazineAmmo <= 0 && PlayerWeaponHandler.Instance != null && PlayerWeaponHandler.Instance.AutoReload)
        {
            ProcessReloadRequest();
        }
    }

    private void ExecuteSingleShot()
    {
        GameObject bullet = _bulletPool.GetPooledObject();

        if (bullet != null)
        {
            bullet.transform.position = _muzzle.position;

            float randomYaw = Random.Range(-Config.SpreadAngle * 0.5f, Config.SpreadAngle * 0.5f);

            Vector3 shotDirection = Vector3.ProjectOnPlane(_muzzle.forward, Vector3.up);

            PlayerMovement movement = PlayerMovement.Instance;
            if (movement != null && movement.HasAimPoint)
            {
                Vector3 cursorDirection = Vector3.ProjectOnPlane(movement.AimPoint - _muzzle.position, Vector3.up);
                if (cursorDirection.sqrMagnitude >= MinimumCursorAimDistance * MinimumCursorAimDistance)
                    shotDirection = cursorDirection;
            }

            if (shotDirection.sqrMagnitude < 0.0001f)
                shotDirection = Vector3.ProjectOnPlane(transform.root.forward, Vector3.up);

            Quaternion aimRotation = Quaternion.LookRotation(shotDirection.normalized, Vector3.up);
            Quaternion spreadRotation = Quaternion.Euler(0f, randomYaw, 0f);
            bullet.transform.rotation = aimRotation * spreadRotation;

            Projectile projectileScript = bullet.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                int finalDamage = GetEffectiveDamageForShot(out bool isCrit);
                projectileScript.InitializeProjectile(finalDamage, Config.MaxTargetPierceCount, isCrit);
            }

            bullet.SetActive(true);
        }
    }

    private int GetEffectiveDamageForShot(out bool isCrit)
    {
        float baseDamage = WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.Damage, Config.Category, Config.Damage);

        float critChance = WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.CritChance, Config.Category, Config.CritChance);
        isCrit = Random.value < critChance;

        if (isCrit)
        {
            float critDamageBonus = WeaponStatsHub.Get(WeaponStatsHub.WeaponStatType.CritDamage, Config.Category, Config.CritDamageBonus);
            baseDamage *= (1f + critDamageBonus);
        }

        return Mathf.RoundToInt(baseDamage);
    }

    private IEnumerator ExecuteBurstRoutine()
    {
        _isBursting = true;

        for (int i = 0; i < Config.BulletsPerBurst; i++)
        {
            if (_state.CurrentMagazineAmmo <= 0) break;

            ExecuteFireCycle();
            yield return new WaitForSeconds(Config.BurstDelay);
        }

        _isBursting = false;
    }

    private IEnumerator ExecuteReloadRoutine()
    {
        _isReloading = true;

        Config.ReloadEvent.Play(_weaponAudioSource);

        yield return new WaitForSeconds(EffectiveReloadDuration);

        int amountNeeded = EffectiveMaxMagazineSize - _state.CurrentMagazineAmmo;
        int amountToTransfer = Mathf.Min(amountNeeded, _state.CurrentReserveAmmo);

        _state.CurrentReserveAmmo -= amountToTransfer;
        _state.CurrentMagazineAmmo += amountToTransfer;
        OnAmmoChanged?.Invoke(this, EventArgs.Empty);

        _isReloading = false;
    }
}