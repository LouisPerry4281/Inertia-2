using UnityEngine;

public class PlayerDamageReceiver : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private float hitInvulnerabilityTime = 0.35f;

    [Header("Runtime")]
    [SerializeField] private float currentHealth;
    [SerializeField] private bool isDead;

    private PlayerCombatManager combatManager;
    private float invulnerableUntil;

    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;

    private void Awake()
    {
        currentHealth = maxHealth;
        combatManager = GetComponent<PlayerCombatManager>();
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        if (isDead)
            return;

        if (combatManager != null && combatManager.IsInvulnerable)
            return;

        if (Time.time < invulnerableUntil)
            return;

        currentHealth = Mathf.Max(0f, currentHealth - damageInfo.damage);
        invulnerableUntil = Time.time + hitInvulnerabilityTime;

        if (currentHealth <= 0f)
        {
            isDead = true;
        }
    }
}
