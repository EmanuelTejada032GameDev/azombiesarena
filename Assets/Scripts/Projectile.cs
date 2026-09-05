using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _speed = 30f;
    [SerializeField] private float _lifeTime = 3f;
    private int _maxTargetPierceCount = 1;

    private float _currentLifeTimer;
    private Vector3 _lastPosition;
    private int _currentTargetPierceCount = 0;

    [SerializeField] private LayerMask _hitLayers;

    [SerializeField] private LayerMask _environmentLayer;

    [Header("Visual Components")]
    [SerializeField] private TrailRenderer _trailRenderer;

    [Header("Game Juice / Impact Prefabs")]
    [SerializeField] private GameObject _environmentImpactPrefab;
    [SerializeField] private GameObject _zombieImpactPrefab;

    private int _damage = 1;

    private readonly RaycastHit[] _hitBuffer = new RaycastHit[5];

    private Collider _lastDamagedCollider;

    public void InitializeProjectile(int damageValue, int maxTargetPierceCount)
    {
        _damage = damageValue;
        _maxTargetPierceCount = maxTargetPierceCount;
        _currentLifeTimer = _lifeTime;
        _lastPosition = transform.position;

        _currentTargetPierceCount = 0;
        _lastDamagedCollider = null;

        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
        }
    }

    private void Update()
    {
        float moveDistance = _speed * Time.deltaTime;
        Vector3 movementDirection = transform.forward;


        int hitCount = Physics.RaycastNonAlloc(_lastPosition, movementDirection, _hitBuffer, moveDistance, _hitLayers);

        if (hitCount > 0)
        {
            SortHitsByDistance(hitCount);

            for (int i = 0; i < hitCount; i++)
            {
                RaycastHit hit = _hitBuffer[i];

                if (((1 << hit.collider.gameObject.layer) & _environmentLayer) != 0)
                {
                    transform.position = hit.point;
                    SpawnImpactEffect(_environmentImpactPrefab, hit.point, hit.normal);
                    Deactivate();
                    return; // Terminate execution immediately
                }

                if (hit.collider == _lastDamagedCollider)
                {
                    continue;
                }

                IDamagable damageable = hit.collider.gameObject.GetComponentInParent<IDamagable>();
                if (damageable != null)
                {
                    damageable.TakeDamage(_damage);
                    SpawnImpactEffect(_zombieImpactPrefab, hit.point, hit.normal);

                    _lastDamagedCollider = hit.collider;
                    _currentTargetPierceCount++;

                    if (_currentTargetPierceCount >= _maxTargetPierceCount)
                    {
                        transform.position = hit.point;
                        Deactivate();
                        return;
                    }
                }
            }
        }

        transform.Translate(Vector3.forward * moveDistance);
        _lastPosition = transform.position;

        _currentLifeTimer -= Time.deltaTime;
        if (_currentLifeTimer <= 0f)
        {
            Deactivate();
        }
    }

    private void SortHitsByDistance(int count)
    {
        for (int i = 0; i < count - 1; i++)
        {
            int closestIndex = i;
            for (int j = i + 1; j < count; j++)
            {
                if (_hitBuffer[j].distance < _hitBuffer[closestIndex].distance)
                {
                    closestIndex = j;
                }
            }

            if (closestIndex != i)
            {
                RaycastHit temp = _hitBuffer[i];
                _hitBuffer[i] = _hitBuffer[closestIndex];
                _hitBuffer[closestIndex] = temp;
            }
        }
    }
  

    private void SpawnImpactEffect(GameObject prefab, Vector3 position, Vector3 normal)
    {
        if (prefab == null) return;
        GameObject impactFX = Instantiate(prefab, position, Quaternion.LookRotation(normal));
        Destroy(impactFX, 1.0f);
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}
