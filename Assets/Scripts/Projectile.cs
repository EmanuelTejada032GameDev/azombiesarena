using UnityEngine;

public class Projectile : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float _speed = 30f;
    [SerializeField] private float _lifeTime = 3f;
    private float _currentLifeTimer;
    private Vector3 _lastPosition;

    [SerializeField] private LayerMask _hitLayers;

    private void OnEnable()
    {
        _currentLifeTimer = _lifeTime;
        _lastPosition = transform.position;
    }

    private void Update()
    {
        float moveDistance = _speed * Time.deltaTime;
        Vector3 movementDirection = transform.forward;

        // Calculate using math operation faster than relying on collider's OnTriggerEnter for high-speed projectiles
        if (Physics.Raycast(_lastPosition, movementDirection, out RaycastHit hit, moveDistance, _hitLayers))
        {
            transform.position = hit.point; 
            HandleImpact(hit.collider);
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

    private void OnTriggerEnter(Collider other)
    {
        if ((_hitLayers.value & (1 << other.gameObject.layer)) > 0)
        {
            HandleImpact(other);
        }
    }

    private void HandleImpact(Collider hitCollider)
    {
        //Debug.Log("Bullet hit: " + hitCollider.gameObject.name + " on layer: " + LayerMask.LayerToName(hitCollider.gameObject.layer));
        Deactivate();
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}