using UnityEngine;

public interface IDamageable
{
    bool CanTakeDamage { get; }
    void TakeDamage(float amount, GameObject source = null);
}
