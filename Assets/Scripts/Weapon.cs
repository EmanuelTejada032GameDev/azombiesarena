using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectPooler _bulletPool;
    [SerializeField] private Transform _muzzle;

    private WeaponInstanceState _state;

    private float _nextFireTime;
    private bool _isBursting;
    private bool _isReloading;

    public WeaponDataConfig Config => _state?.BlueprintConfig;
    public WeaponInstanceState State => _state;
    public bool IsReloading => _isReloading;

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

    public void ProcessFireRequest(bool isTriggerHeld)
    {
        if (Time.time < _nextFireTime || _isBursting || _isReloading || _state == null || _state.CurrentMagazineAmmo <= 0) return;

        switch (Config.FiringMode)
        {
            case WeaponFiringMode.SemiAutomatic:
                ExecuteFireCycle();
                _nextFireTime = Time.time + Config.FireCooldown;
                break;

            case WeaponFiringMode.FullAutomatic:
                if (isTriggerHeld)
                {
                    ExecuteFireCycle();
                    _nextFireTime = Time.time + Config.FireCooldown;
                }
                break;

            case WeaponFiringMode.Burst:
                StartCoroutine(ExecuteBurstRoutine());
                _nextFireTime = Time.time + Config.FireCooldown;
                break;
        }
    }

    public void ProcessReloadRequest()
    {
        if (_isReloading || _state == null || _state.CurrentMagazineAmmo == Config.MaxMagazineSize || _state.CurrentReserveAmmo <= 0) return;

        StartCoroutine(ExecuteReloadRoutine());
    }

    private void ExecuteFireCycle()
    {
        if (_bulletPool == null || _state.CurrentMagazineAmmo <= 0) return;

        _state.CurrentMagazineAmmo--;

        for (int i = 0; i < Config.PelletCount; i++)
        {
            ExecuteSingleShot();
        }
    }

    private void ExecuteSingleShot()
    {
        GameObject bullet = _bulletPool.GetPooledObject();

        if (bullet != null)
        {
            bullet.transform.position = _muzzle.position;

            float randomPitch = Random.Range(-Config.SpreadAngle * 0.5f, Config.SpreadAngle * 0.5f);
            float randomYaw = Random.Range(-Config.SpreadAngle * 0.5f, Config.SpreadAngle * 0.5f);

            Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);
            bullet.transform.rotation = _muzzle.rotation * spreadRotation;

            Projectile projectileScript = bullet.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.InitializeProjectile(Config.Damage);
            }

            bullet.SetActive(true);
        }
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

        yield return new WaitForSeconds(Config.ReloadDuration);

        int amountNeeded = Config.MaxMagazineSize - _state.CurrentMagazineAmmo;
        int amountToTransfer = Mathf.Min(amountNeeded, _state.CurrentReserveAmmo);

        _state.CurrentReserveAmmo -= amountToTransfer;
        _state.CurrentMagazineAmmo += amountToTransfer;

        _isReloading = false;
    }
}
