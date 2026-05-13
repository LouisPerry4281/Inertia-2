using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCombatManager : MonoBehaviour
{
    private enum AttackType
    {
        Light,
        Heavy
    }

    [System.Serializable]
    private struct ComboAttack
    {
        public AttackType type;
        public float damage;
        public float startupTime;
        public float activeTime;
        public float recoveryTime;
        public float hitRadius;
        public float hitDistance;
        public float lungeDistance;
        public float lungeDuration;
        public float knockback;
        public float juiceGain;
        public float hitStop;
    }

    private PlayerManager player;
    private Coroutine activeAction;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    [Header("Combo")]
    [SerializeField] private ComboAttack[] combo =
    {
        new ComboAttack
        {
            type = AttackType.Light,
            damage = 8f,
            startupTime = 0.08f,
            activeTime = 0.12f,
            recoveryTime = 0.18f,
            hitRadius = 0.9f,
            hitDistance = 1.35f,
            lungeDistance = 0.75f,
            lungeDuration = 0.14f,
            knockback = 3f,
            juiceGain = 5f,
            hitStop = 0.04f
        },
        new ComboAttack
        {
            type = AttackType.Light,
            damage = 10f,
            startupTime = 0.07f,
            activeTime = 0.12f,
            recoveryTime = 0.2f,
            hitRadius = 0.95f,
            hitDistance = 1.45f,
            lungeDistance = 0.9f,
            lungeDuration = 0.15f,
            knockback = 4f,
            juiceGain = 6f,
            hitStop = 0.045f
        },
        new ComboAttack
        {
            type = AttackType.Heavy,
            damage = 18f,
            startupTime = 0.14f,
            activeTime = 0.16f,
            recoveryTime = 0.32f,
            hitRadius = 1.15f,
            hitDistance = 1.7f,
            lungeDistance = 1.2f,
            lungeDuration = 0.18f,
            knockback = 7f,
            juiceGain = 10f,
            hitStop = 0.065f
        }
    };

    [SerializeField] private float comboResetTime = 0.8f;
    [SerializeField] private float inputBufferTime = 0.22f;
    [SerializeField] private LayerMask damageableLayers = ~0;

    [Header("Dodge")]
    [SerializeField] private float dodgeDistance = 4f;
    [SerializeField] private float dodgeDuration = 0.22f;
    [SerializeField] private float dodgeInvulnerabilityTime = 0.32f;
    [SerializeField] private float perfectDodgeWindow = 0.16f;
    [SerializeField] private float flowTimeDuration = 1.8f;
    [SerializeField] private float flowTimeScale = 0.35f;
    [SerializeField] private float dodgeJuiceGain = 8f;

    [Header("Runtime")]
    [SerializeField] private int comboIndex;
    [SerializeField] private bool isInvulnerable;
    [SerializeField] private bool isInFlowTime;

    private float lastAttackTime;
    private float bufferedLightUntil;
    private float bufferedHeavyUntil;
    private float perfectDodgeUntil;
    private float defaultFixedDeltaTime;

    public bool IsInvulnerable => isInvulnerable;

    private void Awake()
    {
        player = GetComponent<PlayerManager>();
        defaultFixedDeltaTime = Time.fixedDeltaTime;
    }

    public void HandleAllCombat()
    {
        if (PlayerInputManager.instance == null)
            return;

        CaptureCombatInput();

        if (activeAction != null)
            return;

        if (Time.time - lastAttackTime > comboResetTime)
        {
            comboIndex = 0;
        }

        if (TryConsumeBufferedAttack(out AttackType attackType))
        {
            activeAction = StartCoroutine(PerformAttack(attackType));
            return;
        }

        if (PlayerInputManager.instance.ConsumeDodgeInput())
        {
            activeAction = StartCoroutine(PerformDodge());
        }
    }

    public void NotifyThreatNearMiss()
    {
        if (!isInvulnerable || Time.time > perfectDodgeUntil)
            return;

        if (PlayerJuiceManager.instance != null)
        {
            PlayerJuiceManager.instance.AddJuice(dodgeJuiceGain);
        }

        if (!isInFlowTime)
        {
            StartCoroutine(ApplyFlowTime());
        }
    }

    private void CaptureCombatInput()
    {
        if (PlayerInputManager.instance.ConsumeLightAttackInput())
        {
            bufferedLightUntil = Time.time + inputBufferTime;
        }

        if (PlayerInputManager.instance.ConsumeHeavyAttackInput())
        {
            bufferedHeavyUntil = Time.time + inputBufferTime;
        }
    }

    private bool TryConsumeBufferedAttack(out AttackType attackType)
    {
        if (bufferedLightUntil >= Time.time)
        {
            bufferedLightUntil = 0f;
            attackType = AttackType.Light;
            return true;
        }

        if (bufferedHeavyUntil >= Time.time)
        {
            bufferedHeavyUntil = 0f;
            attackType = AttackType.Heavy;
            return true;
        }

        attackType = AttackType.Light;
        return false;
    }

    private IEnumerator PerformAttack(AttackType requestedType)
    {
        ComboAttack attack = GetNextAttack(requestedType);
        player.isPerformingAction = true;
        player.canMove = false;
        player.canRotate = false;
        hitTargets.Clear();

        FaceCombatDirection();

        if (attack.startupTime > 0f)
        {
            yield return new WaitForSeconds(attack.startupTime);
        }

        Coroutine lunge = StartCoroutine(MoveAlongFacing(attack.lungeDistance, attack.lungeDuration));
        float activeUntil = Time.time + attack.activeTime;
        while (Time.time < activeUntil)
        {
            DetectHits(attack);
            yield return null;
        }

        if (lunge != null)
        {
            StopCoroutine(lunge);
        }

        if (attack.recoveryTime > 0f)
        {
            yield return new WaitForSeconds(attack.recoveryTime);
        }

        lastAttackTime = Time.time;
        player.isPerformingAction = false;
        player.canMove = true;
        player.canRotate = true;
        activeAction = null;
    }

    private ComboAttack GetNextAttack(AttackType requestedType)
    {
        if (combo == null || combo.Length == 0)
        {
            return new ComboAttack { type = requestedType, damage = 10f, hitRadius = 1f, hitDistance = 1.5f };
        }

        for (int attempts = 0; attempts < combo.Length; attempts++)
        {
            ComboAttack attack = combo[comboIndex];
            comboIndex = (comboIndex + 1) % combo.Length;

            if (attack.type == requestedType)
            {
                return attack;
            }
        }

        ComboAttack fallback = combo[0];
        comboIndex = 1 % combo.Length;
        return fallback;
    }

    private IEnumerator PerformDodge()
    {
        player.isPerformingAction = true;
        player.canMove = false;
        player.canRotate = false;
        isInvulnerable = true;
        perfectDodgeUntil = Time.time + perfectDodgeWindow;

        Vector3 dodgeDirection = GetInputWorldDirection();
        if (dodgeDirection == Vector3.zero)
        {
            dodgeDirection = -transform.forward;
        }

        FaceDirection(dodgeDirection);

        float invulnerableUntil = Time.time + dodgeInvulnerabilityTime;
        yield return MoveInDirection(dodgeDirection, dodgeDistance, dodgeDuration);

        while (Time.time < invulnerableUntil)
        {
            yield return null;
        }

        isInvulnerable = false;
        player.isPerformingAction = false;
        player.canMove = true;
        player.canRotate = true;
        activeAction = null;
    }

    private IEnumerator MoveAlongFacing(float distance, float duration)
    {
        yield return MoveInDirection(transform.forward, distance, duration);
    }

    private IEnumerator MoveInDirection(Vector3 direction, float distance, float duration)
    {
        if (player.characterController == null || duration <= 0f || distance <= 0f)
            yield break;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;
            player.characterController.Move(direction.normalized * (distance / duration) * delta);
            yield return null;
        }
    }

    private void DetectHits(ComboAttack attack)
    {
        Vector3 hitCenter = transform.position + transform.forward * attack.hitDistance + Vector3.up;
        Collider[] hits = Physics.OverlapSphere(hitCenter, attack.hitRadius, damageableLayers, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (hit.transform.root == transform.root)
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || hitTargets.Contains(damageable))
                continue;

            hitTargets.Add(damageable);

            Vector3 hitPoint = hit.ClosestPoint(hitCenter);
            Vector3 force = transform.forward * attack.knockback;
            damageable.TakeDamage(new DamageInfo(gameObject, attack.damage, attack.hitStop, attack.juiceGain, hitPoint, force));

            if (PlayerJuiceManager.instance != null)
            {
                PlayerJuiceManager.instance.AddJuice(attack.juiceGain);
            }

            if (attack.hitStop > 0f)
            {
                StartCoroutine(ApplyHitStop(attack.hitStop));
            }
        }
    }

    private IEnumerator ApplyHitStop(float duration)
    {
        float previousTimeScale = Time.timeScale;
        Time.timeScale = 0.05f;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
    }

    private IEnumerator ApplyFlowTime()
    {
        isInFlowTime = true;
        float previousTimeScale = Time.timeScale;
        Time.timeScale = flowTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        yield return new WaitForSecondsRealtime(flowTimeDuration);
        Time.timeScale = previousTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        isInFlowTime = false;
    }

    private void FaceCombatDirection()
    {
        Vector3 inputDirection = GetInputWorldDirection();
        if (inputDirection != Vector3.zero)
        {
            FaceDirection(inputDirection);
        }
    }

    private Vector3 GetInputWorldDirection()
    {
        if (PlayerInputManager.instance == null || PlayerCamera.instance == null)
            return Vector3.zero;

        Vector3 direction = PlayerCamera.instance.transform.forward * PlayerInputManager.instance.verticalInput;
        direction += PlayerCamera.instance.transform.right * PlayerInputManager.instance.horizontalInput;
        direction.y = 0f;
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.zero;
    }

    private void FaceDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.001f)
            return;

        transform.rotation = Quaternion.LookRotation(direction.normalized);
    }

    private void OnDrawGizmosSelected()
    {
        if (combo == null || combo.Length == 0)
            return;

        ComboAttack attack = combo[Mathf.Clamp(comboIndex, 0, combo.Length - 1)];
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + transform.forward * attack.hitDistance + Vector3.up, attack.hitRadius);
    }
}
