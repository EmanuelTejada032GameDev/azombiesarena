using UnityEngine;

public class Grenade : MonoBehaviour
{
    [SerializeField] private float _fuseDuration = 2.5f;
    [SerializeField] private float _explosionRadius = 5f;

    // From the center of the explosion, the damage zones are defined as percentages of the total radius.
    [SerializeField] private float _lethalBlastAreaPercentage = .4f;
    [SerializeField] private float _highDamageBlastAreaPercentage = .7f;

    [SerializeField] private LayerMask _zombieLayer;

    [SerializeField] private GameObject _explosionEffectPrefab;




    private void Start()
    {
        Invoke(nameof(Explode), _fuseDuration);
    }

    private void Explode()
    {
        if (_explosionEffectPrefab != null)
        {
            Instantiate(_explosionEffectPrefab, transform.position, Quaternion.identity);
        }

        Collider[] caughtZombies = Physics.OverlapSphere(transform.position, _explosionRadius, _zombieLayer);

        foreach (Collider zombieCollider in caughtZombies)
        {
            float distance = Vector3.Distance(transform.position, zombieCollider.transform.position);

            // Normalize distance to a clean fraction scale (0.0 at center, 1.0 at outer edge)
            float normalizedDistance = distance / _explosionRadius;

            HealthSystem health = zombieCollider.GetComponentInParent<HealthSystem>();

            Debug.Log($"Zombie: {zombieCollider.name}, Distance: {distance}, Normalized Distance: {normalizedDistance}, helathComponent is null: {health == null}");

            if (health != null)
            {
                float normalZombieMaxHP = 6;
                float finalDamage = 0f;

                // Zone 1: Epicenter Zone (0.0 to 0.4) -> 100% Lethal Strike
                if (normalizedDistance <= _lethalBlastAreaPercentage)
                {
                    finalDamage = normalZombieMaxHP;
                }
                // Zone 2: Mid-Ring Zone (0.41 to 0.7) -> 50% Heavy Chunk Damage
                else if (normalizedDistance <= _highDamageBlastAreaPercentage)
                {
                    finalDamage = normalZombieMaxHP * 0.5f;
                }
                // Zone 3: Outer-Ring Zone (0.71 to 1.0) -> 30% Light Clip Damage
                else
                {
                    finalDamage = normalZombieMaxHP * 0.3f;
                }

         
                health.TakeDamage(finalDamage);
            }

            Rigidbody zombieRb = zombieCollider.GetComponentInParent<Rigidbody>();
            if (zombieRb != null)
            {
                zombieRb.AddExplosionForce(500f, transform.position, _explosionRadius, 1f, ForceMode.Impulse);
            }
        }

        Destroy(gameObject);
    }


    private void OnDrawGizmosSelected()
    {
        // 1. Inner Circle: Epicenter Zone (Red)
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius * _lethalBlastAreaPercentage);

        // 2. Middle Circle: Mid-Ring Zone (Orange)
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius * _highDamageBlastAreaPercentage);

        // 3. Outer Circle: Full Limit (Yellow)
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}
