using System.Collections;
using UnityEngine;

public class Weapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectPooler _bulletPool;
    [SerializeField] private Transform _muzzle;

    [Header("Configuration Asset")]
    [SerializeField] private WeaponDataConfig _config;

    private float _nextFireTime;
    private bool _isBursting;

    public WeaponDataConfig Config => _config;

    public void InitializeWeapon(WeaponDataConfig configAsset, ObjectPooler matchingPool)
    {
        _config = configAsset;
        _bulletPool = matchingPool;
        _nextFireTime = 0f;
        _isBursting = false;
    }

    public void ProcessFireRequest(bool isTriggerHeld)
    {
        if (Time.time < _nextFireTime || _isBursting || _config == null) return;

        switch (_config.FiringMode)
        {
            case WeaponFiringMode.SemiAutomatic:
                ExecuteFireCycle();
                _nextFireTime = Time.time + _config.FireCooldown;
                break;

            case WeaponFiringMode.FullAutomatic:
                if (isTriggerHeld)
                {
                    ExecuteFireCycle();
                    _nextFireTime = Time.time + _config.FireCooldown;
                }
                break;

            case WeaponFiringMode.Burst:
                StartCoroutine(ExecuteBurstRoutine());
                _nextFireTime = Time.time + _config.FireCooldown;
                break;
        }
    }


    private void ExecuteFireCycle()
    {
        if (_bulletPool == null) return;

        for (int i = 0; i < _config.PelletCount; i++)
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

            // Calculate random deviation within the configured spread angle cone
            float randomPitch = Random.Range(-_config.SpreadAngle * 0.5f, _config.SpreadAngle * 0.5f);
            float randomYaw = Random.Range(-_config.SpreadAngle * 0.5f, _config.SpreadAngle * 0.5f);

            // Combine muzzle orientation with random offset
            Quaternion spreadRotation = Quaternion.Euler(randomPitch, randomYaw, 0f);
            bullet.transform.rotation = _muzzle.rotation * spreadRotation;

            // Initialize damage and wake up the bullet
            Projectile projectileScript = bullet.GetComponent<Projectile>();
            if (projectileScript != null)
            {
                projectileScript.InitializeProjectile(_config.Damage);
            }

            bullet.SetActive(true);
        }
    }

    private IEnumerator ExecuteBurstRoutine()
    {
        _isBursting = true;

        for (int i = 0; i < _config.BulletsPerBurst; i++)
        {
            ExecuteFireCycle(); // Supports firing multi-pellet rounds inside bursts!
            yield return new WaitForSeconds(_config.BurstDelay);
        }

        _isBursting = false;
    }
}
