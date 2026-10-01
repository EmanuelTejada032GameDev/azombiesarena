using UnityEngine;

public class ZombieAnimationRelay : MonoBehaviour
{
    private Zombie _zombie;

    private void Awake()
    {
        _zombie = GetComponentInParent<Zombie>();
    }

    public void OnAttackHit()
    {
        if (_zombie != null) _zombie.OnAttackHit();
    }

    public void OnAttackEnd()
    {
        if (_zombie != null) _zombie.OnAttackEnd();
    }

    public void OnHitReactEnd()
    {
        if (_zombie != null) _zombie.OnHitReactEnd();
    }
}