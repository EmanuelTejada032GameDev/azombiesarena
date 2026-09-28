public interface IDamagable
{
    void TakeDamage(int amount);
    void TakeDamage(int amount, bool isCrit);
    void Heal(int amount);
    void Die();
}