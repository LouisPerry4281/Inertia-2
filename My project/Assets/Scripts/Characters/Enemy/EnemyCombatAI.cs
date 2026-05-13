using System.Collections;
using UnityEngine;

public class EnemyCombatAI : MonoBehaviour
{
    private enum EnemyState
    {
        Idle,
        Chase,
        Strafe,
        Telegraph,
        Attack,
        Recover
    }

    [System.Serializable]
    private struct EnemyAttack
    {
        public string name;
        public float damage;
        public float attackRange;
        public float hitRadius;
        public float telegraphTime;
        public float lungeDistance;
        public float lungeDuration;
        public float activeTime;
        public float recoveryTime;
        public float knockback;
    }

    [Header("Targeting")]
    [SerializeField] private PlayerManager target;
    [SerializeField] private float aggroRange = 14f;
    [SerializeField] private float preferredRange = 4.5f;
    [SerializeField] private float disengageRange = 20f;
    [SerializeField] private LayerMask playerLayers = ~0;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float strafeSpeed = 2.6f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float strafeDurationMin = 0.7f;
    [SerializeField] private float strafeDurationMax = 1.5f;
    [SerializeField] private float gravity = -18f;

    [Header("Tempo")]
    [SerializeField] private float decisionDelayMin = 0.25f;
    [SerializeField] private float decisionDelayMax = 0.7f;
    [SerializeField] private float comboChance = 0.35f;

    [Header("Attacks")]
    [SerializeField] private EnemyAttack[] attacks =
    {
        new EnemyAttack
        {
            name = "Claw Swipe",
            damage = 10f,
            attackRange = 2.2f,
            hitRadius = 1f,
            telegraphTime = 0.45f,
            lungeDistance = 1.5f,
            lungeDuration = 0.16f,
            activeTime = 0.16f,
            recoveryTime = 0.5f,
            knockback = 5f
        },
        new EnemyAttack
        {
            name = "Stinger",
            damage = 14f,
            attackRange = 5.2f,
            hitRadius = 0.9f,
            telegraphTime = 0.6f,
            lungeDistance = 4.2f,
            lungeDuration = 0.28f,
            activeTime = 0.2f,
            recoveryTime = 0.7f,
            knockback = 8f
        }
    };

    [Header("Telegraph")]
    [SerializeField] private Renderer[] telegraphRenderers;
    [SerializeField] private Color telegraphColor = Color.red;
    [SerializeField] private float telegraphPulseSpeed = 18f;

    [Header("Runtime")]
    [SerializeField] private EnemyState state;
    [SerializeField] private float distanceToTarget;

    private CharacterController characterController;
    private Rigidbody enemyRigidbody;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine actionRoutine;
    private Vector3 verticalVelocity;
    private float nextDecisionTime;
    private int strafeDirection = 1;
    private Color defaultColor = Color.white;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public string CurrentState => state.ToString();

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        enemyRigidbody = GetComponent<Rigidbody>();
        propertyBlock = new MaterialPropertyBlock();

        if (telegraphRenderers == null || telegraphRenderers.Length == 0)
        {
            telegraphRenderers = GetComponentsInChildren<Renderer>();
        }

        if (telegraphRenderers.Length > 0 && telegraphRenderers[0].sharedMaterial != null)
        {
            defaultColor = telegraphRenderers[0].sharedMaterial.HasProperty(BaseColorId)
                ? telegraphRenderers[0].sharedMaterial.GetColor(BaseColorId)
                : telegraphRenderers[0].sharedMaterial.color;
        }
    }

    private void Update()
    {
        AcquireTarget();

        if (target == null)
        {
            state = EnemyState.Idle;
            return;
        }

        distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
        FaceTarget();
        ApplyGravity();

        if (actionRoutine != null)
            return;

        if (distanceToTarget > disengageRange)
        {
            state = EnemyState.Idle;
            return;
        }

        if (distanceToTarget > aggroRange)
        {
            state = EnemyState.Idle;
            return;
        }

        if (distanceToTarget > preferredRange)
        {
            state = EnemyState.Chase;
            MoveTowards(target.transform.position, moveSpeed);
            return;
        }

        if (Time.time < nextDecisionTime)
        {
            state = EnemyState.Strafe;
            StrafeAroundTarget();
            return;
        }

        EnemyAttack attack = ChooseAttack();
        if (distanceToTarget <= attack.attackRange)
        {
            actionRoutine = StartCoroutine(PerformAttack(attack));
        }
        else
        {
            nextDecisionTime = Time.time + Random.Range(decisionDelayMin, decisionDelayMax);
        }
    }

    private void AcquireTarget()
    {
        if (target != null)
            return;

        target = FindAnyObjectByType<PlayerManager>();
    }

    private void FaceTarget()
    {
        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        if (toTarget.sqrMagnitude <= 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }

    private void MoveTowards(Vector3 position, float speed)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0f;
        Move(direction.normalized * speed);
    }

    private void StrafeAroundTarget()
    {
        Vector3 toTarget = target.transform.position - transform.position;
        toTarget.y = 0f;
        Vector3 tangent = Vector3.Cross(Vector3.up, toTarget.normalized) * strafeDirection;
        Move(tangent * strafeSpeed);
    }

    private IEnumerator PerformAttack(EnemyAttack attack)
    {
        state = EnemyState.Telegraph;
        float telegraphUntil = Time.time + attack.telegraphTime;
        while (Time.time < telegraphUntil)
        {
            PulseTelegraph(telegraphUntil - Time.time, attack.telegraphTime);
            FaceTarget();
            yield return null;
        }

        ClearTelegraph();

        state = EnemyState.Attack;
        float elapsed = 0f;
        bool hasHit = false;

        while (elapsed < attack.lungeDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;
            Move(transform.forward * (attack.lungeDistance / attack.lungeDuration));
            if (!hasHit)
            {
                hasHit = TryHitPlayer(attack);
            }
            yield return null;
        }

        float activeUntil = Time.time + attack.activeTime;
        while (Time.time < activeUntil)
        {
            if (!hasHit)
            {
                hasHit = TryHitPlayer(attack);
            }
            yield return null;
        }

        state = EnemyState.Recover;
        yield return new WaitForSeconds(attack.recoveryTime);

        if (hasHit && Random.value < comboChance)
        {
            EnemyAttack nextAttack = ChooseAttack();
            if (distanceToTarget <= nextAttack.attackRange + 1f)
            {
                actionRoutine = StartCoroutine(PerformAttack(nextAttack));
                yield break;
            }
        }

        strafeDirection = Random.value < 0.5f ? -1 : 1;
        nextDecisionTime = Time.time + Random.Range(strafeDurationMin, strafeDurationMax);
        actionRoutine = null;
    }

    private bool TryHitPlayer(EnemyAttack attack)
    {
        Vector3 hitCenter = transform.position + transform.forward * attack.attackRange + Vector3.up;
        Collider[] hits = Physics.OverlapSphere(hitCenter, attack.hitRadius, playerLayers, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            PlayerCombatManager playerCombat = hit.GetComponentInParent<PlayerCombatManager>();
            if (playerCombat != null && playerCombat.IsInvulnerable)
            {
                playerCombat.NotifyThreatNearMiss();
                return false;
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null)
                continue;

            Vector3 hitPoint = hit.ClosestPoint(hitCenter);
            Vector3 force = transform.forward * attack.knockback;
            damageable.TakeDamage(new DamageInfo(gameObject, attack.damage, 0f, 0f, hitPoint, force));
            return true;
        }

        return false;
    }

    private EnemyAttack ChooseAttack()
    {
        if (attacks == null || attacks.Length == 0)
        {
            return new EnemyAttack
            {
                name = "Default",
                damage = 10f,
                attackRange = 2f,
                hitRadius = 1f,
                telegraphTime = 0.45f,
                lungeDistance = 1.5f,
                lungeDuration = 0.18f,
                activeTime = 0.15f,
                recoveryTime = 0.5f,
                knockback = 4f
            };
        }

        return attacks[Random.Range(0, attacks.Length)];
    }

    private void Move(Vector3 velocity)
    {
        Vector3 finalVelocity = velocity + verticalVelocity;

        if (characterController != null)
        {
            characterController.Move(finalVelocity * Time.deltaTime);
            return;
        }

        if (enemyRigidbody != null && !enemyRigidbody.isKinematic)
        {
            enemyRigidbody.MovePosition(enemyRigidbody.position + finalVelocity * Time.deltaTime);
            return;
        }

        transform.position += finalVelocity * Time.deltaTime;
    }

    private void ApplyGravity()
    {
        if (characterController != null && characterController.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = -2f;
            return;
        }

        verticalVelocity.y += gravity * Time.deltaTime;
    }

    private void PulseTelegraph(float remainingTime, float telegraphTime)
    {
        float normalized = telegraphTime <= 0f ? 1f : 1f - Mathf.Clamp01(remainingTime / telegraphTime);
        float pulse = Mathf.Abs(Mathf.Sin(Time.time * telegraphPulseSpeed)) * normalized;
        Color color = Color.Lerp(defaultColor, telegraphColor, pulse);

        foreach (Renderer telegraphRenderer in telegraphRenderers)
        {
            if (telegraphRenderer == null)
                continue;

            telegraphRenderer.GetPropertyBlock(propertyBlock);
            if (telegraphRenderer.sharedMaterial != null && telegraphRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }
            else
            {
                propertyBlock.SetColor(ColorId, color);
            }

            telegraphRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void ClearTelegraph()
    {
        foreach (Renderer telegraphRenderer in telegraphRenderers)
        {
            if (telegraphRenderer == null)
                continue;

            telegraphRenderer.GetPropertyBlock(propertyBlock);
            if (telegraphRenderer.sharedMaterial != null && telegraphRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, defaultColor);
            }
            else
            {
                propertyBlock.SetColor(ColorId, defaultColor);
            }

            telegraphRenderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        if (attacks == null || attacks.Length == 0)
            return;

        Gizmos.color = Color.red;
        EnemyAttack attack = attacks[0];
        Gizmos.DrawWireSphere(transform.position + transform.forward * attack.attackRange + Vector3.up, attack.hitRadius);
    }
}
