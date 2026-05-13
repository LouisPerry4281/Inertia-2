using UnityEngine;

public class CharacterCombatTarget : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath;

    private float currentHealth;
    private Rigidbody targetRigidbody;

    private void Awake()
    {
        currentHealth = maxHealth;
        targetRigidbody = GetComponent<Rigidbody>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        currentHealth = Mathf.Max(0f, currentHealth - damageInfo.damage);

        if (targetRigidbody != null && damageInfo.force.sqrMagnitude > 0f)
        {
            targetRigidbody.AddForceAtPosition(damageInfo.force, damageInfo.hitPoint, ForceMode.Impulse);
        }
        print(currentHealth);
        if (destroyOnDeath && currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }
}
