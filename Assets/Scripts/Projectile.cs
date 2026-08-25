using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _speed = 30f;
    [SerializeField] private float _lifeTime = 3f;
    private float _currentLifeTimer;
    private Vector3 _lastPosition;

    [SerializeField] private LayerMask _hitLayers;


    [Header("VFXs Settings")]
    [SerializeField] private GameObject _environmentImpactPrefab;
    [SerializeField] private GameObject _zombieImpactPrefab;
    [SerializeField] private TrailRenderer _trailRenderer;



    private void OnEnable()
    {
        _currentLifeTimer = _lifeTime;
        _lastPosition = transform.position;
        if (_trailRenderer != null)
        {
            _trailRenderer.Clear();
        }
    }

    private void Update()
    {
        float moveDistance = _speed * Time.deltaTime;
        Vector3 movementDirection = transform.forward;

        // Calculate using math operation faster than relying on collider's OnTriggerEnter for high-speed projectiles
        if (Physics.Raycast(_lastPosition, movementDirection, out RaycastHit hit, moveDistance, _hitLayers))
        {
            transform.position = hit.point; 
            HandleImpact(hit.collider, hit.point, hit.normal);
            return; 
        }

        transform.Translate(Vector3.forward * moveDistance);

        _lastPosition = transform.position;

        _currentLifeTimer -= Time.deltaTime;
        if (_currentLifeTimer <= 0f)
        {
            Deactivate();
        }
    }

 
    private void HandleImpact(Collider hitCollider, Vector3 hitPoint, Vector3 hitNormal)
    {
        IDamagable damageable = hitCollider.gameObject.GetComponentInParent<IDamagable>();

        if (damageable != null)
        {
            damageable.TakeDamage(1);
            SpawnImpactEffect(_zombieImpactPrefab, hitPoint, hitNormal);
        }
        else
        {
            SpawnImpactEffect(_environmentImpactPrefab, hitPoint, hitNormal);
        }

        Deactivate();
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