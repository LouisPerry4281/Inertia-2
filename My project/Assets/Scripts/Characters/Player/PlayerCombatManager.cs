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
        public string moveName;
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
        public float launchHeight;
        public float juiceGain;
        public float hitStop;
        public float styleValue;
        public bool launcher;
        public bool aerial;
        public bool finisher;
        public bool dash;
    }

    private PlayerManager player;
    private Coroutine activeAction;
    private readonly HashSet<IDamageable> hitTargets = new HashSet<IDamageable>();

    [Header("Combo")]
    [SerializeField] private ComboAttack[] combo =
    {
        new ComboAttack
        {
            moveName = "Electric Heel",
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
            launchHeight = 0f,
            juiceGain = 6f,
            hitStop = 0.035f,
            styleValue = 38f
        },
        new ComboAttack
        {
            moveName = "Break Step",
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
            launchHeight = 0f,
            juiceGain = 7f,
            hitStop = 0.04f,
            styleValue = 48f
        },
        new ComboAttack
        {
            moveName = "Electric Launcher",
            type = AttackType.Heavy,
            damage = 14f,
            startupTime = 0.11f,
            activeTime = 0.14f,
            recoveryTime = 0.28f,
            hitRadius = 1.05f,
            hitDistance = 1.55f,
            lungeDistance = 1f,
            lungeDuration = 0.16f,
            knockback = 3.5f,
            launchHeight = 8f,
            juiceGain = 8f,
            hitStop = 0.06f,
            styleValue = 85f,
            launcher = true
        },
        new ComboAttack
        {
            moveName = "Juicy Finisher",
            type = AttackType.Heavy,
            damage = 24f,
            startupTime = 0.16f,
            activeTime = 0.18f,
            recoveryTime = 0.38f,
            hitRadius = 1.3f,
            hitDistance = 1.85f,
            lungeDistance = 1.45f,
            lungeDuration = 0.2f,
            knockback = 10f,
            launchHeight = 3f,
            juiceGain = 10f,
            hitStop = 0.09f,
            styleValue = 155f,
            finisher = true
        }
    };

    [Header("Air Combat")]
    [SerializeField] private ComboAttack aerialLightFollowUp =
        new ComboAttack
        {
            moveName = "Aerial Electric Flurry",
            type = AttackType.Light,
            damage = 7f,
            startupTime = 0.04f,
            activeTime = 0.18f,
            recoveryTime = 0.12f,
            hitRadius = 1.15f,
            hitDistance = 1.35f,
            lungeDistance = 0.65f,
            lungeDuration = 0.12f,
            knockback = 1.5f,
            launchHeight = 0f,
            juiceGain = 4f,
            hitStop = 0.035f,
            styleValue = 95f,
            aerial = true
        };
    [SerializeField] private ComboAttack aerialFollowUp =
        new ComboAttack
        {
            moveName = "Aerial Kick Breaker",
            type = AttackType.Heavy,
            damage = 18f,
            startupTime = 0.05f,
            activeTime = 0.2f,
            recoveryTime = 0.2f,
            hitRadius = 1.2f,
            hitDistance = 1.55f,
            lungeDistance = 1.8f,
            lungeDuration = 0.18f,
            knockback = 6f,
            launchHeight = 2.5f,
            juiceGain = 6f,
            hitStop = 0.06f,
            styleValue = 145f,
            aerial = true
        };
    [SerializeField] private float aerialFollowUpWindow = 0.95f;
    [SerializeField] private float aerialRiseDistance = 1.55f;
    [SerializeField] private float aerialRiseDuration = 0.16f;
    [SerializeField] private float aerialHangTime = 0.16f;
    [SerializeField] private float aerialCameraShake = 0.32f;
    [SerializeField] private float aerialTargetAimHeight = 0.75f;
    [SerializeField] private float aerialTargetCatchDistance = 2.2f;
    [SerializeField] private float aerialTrackedHitRadius = 1.65f;
    [SerializeField] private int lowJuiceMaxAerialChainCount = 2;
    [SerializeField] private int midJuiceMaxAerialChainCount = 4;
    [SerializeField] private int highJuiceMaxAerialChainCount = 6;
    [SerializeField] private int overdriveMaxAerialChainCount = 8;
    [SerializeField] private float aerialChainRiseBonus = 0.25f;
    [SerializeField] private float aerialChainLaunchBonus = 0.75f;
    [SerializeField] private float aerialChainWindow = 0.85f;
    [SerializeField] private float aerialCarryForwardOffset = 1.35f;
    [SerializeField] private float aerialCarryHeightOffset = 0.25f;
    [SerializeField] private float lowJuiceAerialSuspendTime = 0.12f;
    [SerializeField] private float midJuiceAerialSuspendTime = 0.2f;
    [SerializeField] private float highJuiceAerialSuspendTime = 0.3f;
    [SerializeField] private float overdriveAerialSuspendTime = 0.45f;
    [SerializeField] private float aerialSuspendHeight = 0.45f;

    [Header("Dash Attack")]
    [SerializeField] private ComboAttack dashAttack =
        new ComboAttack
        {
            moveName = "Dash Breaker",
            type = AttackType.Light,
            damage = 13f,
            startupTime = 0.04f,
            activeTime = 0.16f,
            recoveryTime = 0.24f,
            hitRadius = 1.05f,
            hitDistance = 1.2f,
            lungeDistance = 3.7f,
            lungeDuration = 0.2f,
            knockback = 5.5f,
            launchHeight = 1f,
            juiceGain = 7f,
            hitStop = 0.045f,
            styleValue = 105f,
            dash = true
        };
    [SerializeField] private float dashAttackWindow = 0.35f;
    [SerializeField] private float dashAttackTargetRange = 8f;
    [SerializeField] private float dashAttackTargetCone = 80f;
    [SerializeField] private float dashAttackTargetAngleWeight = 0.25f;
    [SerializeField] private float dashAttackCatchDistance = 2.4f;

    [Header("Armor Pressure")]
    [SerializeField] private float lightArmorDamage = 8f;
    [SerializeField] private float heavyArmorDamage = 42f;
    [SerializeField] private float dashArmorDamage = 16f;
    [SerializeField] private float aerialArmorDamage = 18f;
    [SerializeField] private float finisherArmorDamage = 80f;
    [SerializeField] private float armorBreakStunDuration = 0.65f;
    [SerializeField] private float finisherStunDuration = 0.9f;

    [SerializeField] private float comboResetTime = 0.95f;
    [SerializeField] private float inputBufferTime = 0.25f;
    [SerializeField] private LayerMask damageableLayers = ~0;

    [Header("Style Assist")]
    [SerializeField] private float targetAssistRange = 7f;
    [SerializeField] private float targetAssistCone = 90f;
    [SerializeField] private float targetAssistPull = 0.45f;
    [SerializeField] private float dodgeOffsetGraceTime = 0.55f;
    [SerializeField] private float dodgeCancelRecoveryWindow = 0.1f;
    [SerializeField] private float finisherCameraShake = 0.55f;
    [SerializeField] private float normalHitCameraShake = 0.18f;
    [SerializeField] private float finisherFovKick = 6f;

    [Header("Dodge")]
    [SerializeField] private float dodgeDistance = 4f;
    [SerializeField] private float dodgeDuration = 0.22f;
    [SerializeField] private float dodgeInvulnerabilityTime = 0.32f;
    [SerializeField] private float perfectDodgeWindow = 0.28f;
    [SerializeField] private float flowTimeDuration = 1.8f;
    [SerializeField] private float flowTimeScale = 0.35f;
    [SerializeField] private float dodgeJuiceGain = 8f;

    [Header("Witch Time")]
    [SerializeField] private float flowStateDamageMultiplier = 1.45f;
    [SerializeField] private float flowStateJuiceMultiplier = 1.35f;
    [SerializeField] private float flowStateHitStopBonus = 0.025f;

    [Header("Runtime")]
    [SerializeField] private int comboIndex;
    [SerializeField] private bool isInvulnerable;
    [SerializeField] private bool isInFlowTime;
    [SerializeField] private bool isInAerialFollowUp;
    [SerializeField] private bool isDodging;
    [SerializeField] private bool isInHitStop;
    [SerializeField] private int aerialChainCount;

    private float lastAttackTime;
    private float bufferedLightUntil;
    private float bufferedHeavyUntil;
    private float perfectDodgeUntil;
    private float preserveComboUntil;
    private float aerialFollowUpUntil;
    private float dashAttackUntil;
    private float flowTimeUntil;
    private float hitStopUntil;
    private float defaultFixedDeltaTime;
    private bool canDodgeCancel;
    private bool dashAttackQueued;
    private CharacterCombatTarget aerialTarget;
    private CharacterCombatTarget activeDashTarget;

    public bool IsInvulnerable => isInvulnerable;
    public bool IsInFlow => isInFlowTime;
    public float FlowNormalized => !isInFlowTime || flowTimeDuration <= 0f ? 0f : Mathf.Clamp01((flowTimeUntil - Time.unscaledTime) / flowTimeDuration);

    private void Awake()
    {
        player = GetComponent<PlayerManager>();
        defaultFixedDeltaTime = Time.fixedDeltaTime;
        ResetCombatTimeScale();
    }

    private void OnDisable()
    {
        ResetCombatTimeScale();
    }

    public void HandleAllCombat()
    {
        if (PlayerInputManager.instance == null)
            return;

        CaptureCombatInput();

        if (activeAction != null)
        {
            if (canDodgeCancel && PlayerInputManager.instance.ConsumeDodgeInput())
            {
                StopCoroutine(activeAction);
                activeAction = StartCoroutine(PerformDodge(true));
            }

            return;
        }

        if (Time.time - lastAttackTime > comboResetTime && Time.time > preserveComboUntil)
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
            activeAction = StartCoroutine(PerformDodge(false));
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

        player.playerStyleManager?.RegisterPerfectDodge();

        if (!isInFlowTime)
        {
            PlayerCamera.instance?.AddCombatImpulse(0.4f, 5f);
            StartCoroutine(ApplyFlowTime());
        }
    }

    private void CaptureCombatInput()
    {
        if (PlayerInputManager.instance.ConsumeLightAttackInput())
        {
            bufferedLightUntil = Time.time + inputBufferTime;
            TryQueueDashAttack();
        }

        if (PlayerInputManager.instance.ConsumeHeavyAttackInput())
        {
            bufferedHeavyUntil = Time.time + inputBufferTime;
            TryQueueDashAttack();
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
        bool isAerialAttack = attack.aerial;
        bool isDashAttack = attack.dash;
        if (isAerialAttack)
        {
            aerialChainCount = Mathf.Clamp(aerialChainCount + 1, 1, GetMaxAerialChainCount());
        }
        else if (!attack.launcher && Time.time > aerialFollowUpUntil)
        {
            aerialChainCount = 0;
        }

        bool liftsAerialChain = IsAerialLiftAttack(attack);

        player.isPerformingAction = true;
        player.canMove = false;
        player.canRotate = false;
        canDodgeCancel = false;
        isInAerialFollowUp = isAerialAttack;
        hitTargets.Clear();

        FaceCombatDirection();
        CharacterCombatTarget assistedTarget = GetAssistedTarget(isAerialAttack, isDashAttack);
        if (assistedTarget != null)
        {
            FaceDirection(assistedTarget.transform.position - transform.position);
        }

        if (isAerialAttack)
        {
            PlayerCamera.instance?.AddCombatImpulse(aerialCameraShake, 3.5f);
            player.playerLocomotion?.ResetVerticalVelocity();
            float chainRiseDistance = liftsAerialChain ? aerialRiseDistance + GetAerialChainBonus(aerialChainRiseBonus) : 0f;
            CarryAerialTargetWithPlayer(assistedTarget, chainRiseDistance, attack);
            if (chainRiseDistance > 0f)
            {
                yield return MoveInDirection(Vector3.up, chainRiseDistance, aerialRiseDuration);
            }
            if (aerialHangTime > 0f)
            {
                yield return new WaitForSeconds(aerialHangTime);
            }
        }

        if (attack.startupTime > 0f)
        {
            yield return new WaitForSeconds(attack.startupTime);
        }

        Vector3 lungeDirection = GetAttackLungeDirection(assistedTarget, isAerialAttack);
        Coroutine lunge = StartCoroutine(MoveInDirection(lungeDirection, GetAttackLungeDistance(attack), attack.lungeDuration));
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

        float recoveryTime = GetAttackRecoveryTime(attack);
        float dodgeCancelWindow = GetDodgeCancelRecoveryWindow(recoveryTime);
        if (recoveryTime > 0f)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, recoveryTime - dodgeCancelWindow));
            canDodgeCancel = true;
            yield return new WaitForSeconds(Mathf.Min(recoveryTime, dodgeCancelWindow));
            canDodgeCancel = false;
        }

        lastAttackTime = Time.time;
        player.isPerformingAction = false;
        player.canMove = true;
        player.canRotate = true;
        canDodgeCancel = false;
        isInAerialFollowUp = false;
        activeDashTarget = null;
        activeAction = null;
    }

    private ComboAttack GetNextAttack(AttackType requestedType)
    {
        if (dashAttackQueued)
        {
            dashAttackQueued = false;
            dashAttackUntil = 0f;
            return dashAttack;
        }

        if (Time.time <= aerialFollowUpUntil && aerialTarget != null && aerialChainCount < GetMaxAerialChainCount())
        {
            aerialFollowUpUntil = 0f;
            return requestedType == AttackType.Light ? aerialLightFollowUp : aerialFollowUp;
        }

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

    private IEnumerator PerformDodge(bool fromCancel)
    {
        player.isPerformingAction = true;
        player.canMove = false;
        player.canRotate = false;
        canDodgeCancel = false;
        isDodging = true;
        isInAerialFollowUp = false;
        aerialChainCount = 0;
        isInvulnerable = true;
        perfectDodgeUntil = Time.time + perfectDodgeWindow;
        preserveComboUntil = Time.time + dodgeOffsetGraceTime;

        Vector3 dodgeDirection = GetInputWorldDirection();
        if (dodgeDirection == Vector3.zero)
        {
            dodgeDirection = -transform.forward;
        }

        FaceDirection(dodgeDirection);
        PlayerCamera.instance?.AddCombatImpulse(fromCancel ? 0.16f : 0.08f, 1.5f);

        float invulnerableUntil = Time.time + dodgeInvulnerabilityTime;
        yield return MoveInDirection(dodgeDirection, dodgeDistance, dodgeDuration);

        while (Time.time < invulnerableUntil)
        {
            yield return null;
        }

        isInvulnerable = false;
        isDodging = false;
        dashAttackUntil = Time.time + dashAttackWindow;
        player.isPerformingAction = false;
        player.canMove = true;
        player.canRotate = true;
        canDodgeCancel = false;
        activeAction = null;
    }

    private IEnumerator MoveAlongFacing(float distance, float duration)
    {
        yield return MoveInDirection(transform.forward, distance, duration);
    }

    private Vector3 GetAttackLungeDirection(CharacterCombatTarget assistedTarget, bool canTrackVertical = false)
    {
        if (assistedTarget == null)
            return transform.forward;

        Vector3 targetPosition = assistedTarget.transform.position + Vector3.up * aerialTargetAimHeight;
        Vector3 toTarget = targetPosition - transform.position;
        if (!canTrackVertical)
        {
            toTarget.y = 0f;
        }

        if (toTarget.sqrMagnitude <= 0.001f)
            return transform.forward;

        Vector3 assistedDirection = Vector3.Slerp(transform.forward, toTarget.normalized, targetAssistPull);
        if (!canTrackVertical)
        {
            assistedDirection.y = 0f;
        }

        return assistedDirection.normalized;
    }

    private CharacterCombatTarget FindBestCombatTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, targetAssistRange, damageableLayers, QueryTriggerInteraction.Collide);
        CharacterCombatTarget bestTarget = null;
        float bestScore = float.MaxValue;

        foreach (Collider hit in hits)
        {
            if (hit.transform.root == transform.root)
                continue;

            CharacterCombatTarget target = hit.GetComponentInParent<CharacterCombatTarget>();
            if (target == null)
                continue;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.001f)
                continue;

            float angle = Vector3.Angle(transform.forward, toTarget.normalized);
            if (angle > targetAssistCone * 0.5f)
                continue;

            float score = toTarget.sqrMagnitude + angle * 0.08f;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
    }

    private CharacterCombatTarget GetAssistedTarget(bool isAerialAttack, bool isDashAttack)
    {
        if (isAerialAttack && aerialTarget != null)
            return aerialTarget;

        if (isDashAttack)
        {
            activeDashTarget = FindBestDashAttackTarget();
            return activeDashTarget;
        }

        return FindBestCombatTarget();
    }

    private CharacterCombatTarget FindBestDashAttackTarget()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, GetDashAttackTargetRange(), damageableLayers, QueryTriggerInteraction.Collide);
        CharacterCombatTarget bestTarget = null;
        float bestScore = float.MaxValue;
        Vector3 facing = transform.forward;
        facing.y = 0f;
        facing = facing.sqrMagnitude > 0.001f ? facing.normalized : Vector3.forward;

        foreach (Collider hit in hits)
        {
            if (hit.transform.root == transform.root)
                continue;

            CharacterCombatTarget target = hit.GetComponentInParent<CharacterCombatTarget>();
            if (target == null)
                continue;

            Vector3 toTarget = target.transform.position - transform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude <= 0.001f)
                continue;

            float angle = Vector3.Angle(facing, toTarget.normalized);
            if (angle > dashAttackTargetCone * 0.5f)
                continue;

            float score = angle + toTarget.sqrMagnitude * dashAttackTargetAngleWeight;
            if (score < bestScore)
            {
                bestScore = score;
                bestTarget = target;
            }
        }

        return bestTarget;
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
        Vector3 hitCenter = GetHitCenter(attack);
        float hitRadius = GetHitRadius(attack);
        Collider[] hits = Physics.OverlapSphere(hitCenter, hitRadius, damageableLayers, QueryTriggerInteraction.Collide);

        foreach (Collider hit in hits)
        {
            if (hit.transform.root == transform.root)
                continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || hitTargets.Contains(damageable))
                continue;
            
            SoundManager.Instance.Play("Impact 1");

            hitTargets.Add(damageable);
            CharacterCombatTarget combatTarget = hit.GetComponentInParent<CharacterCombatTarget>();

            Vector3 hitPoint = hit.ClosestPoint(hitCenter);
            Vector3 force = transform.forward * attack.knockback;
            Vector3 launch = Vector3.up * GetLaunchHeight(attack);
            string moveName = GetMoveName(attack);
            float repetitionJuiceMultiplier = player.playerStyleManager != null ? player.playerStyleManager.GetMoveRepetitionMultiplier(moveName) : 1f;
            float damage = isInFlowTime ? attack.damage * flowStateDamageMultiplier : attack.damage;
            float juiceGain = isInFlowTime ? attack.juiceGain * flowStateJuiceMultiplier : attack.juiceGain;
            juiceGain *= repetitionJuiceMultiplier;
            float hitStop = isInFlowTime ? attack.hitStop + flowStateHitStopBonus : attack.hitStop;
            damageable.TakeDamage(new DamageInfo(
                gameObject,
                damage,
                hitStop,
                juiceGain,
                hitPoint,
                force,
                launch,
                GetArmorDamage(attack),
                RequiresBrokenArmorForStagger(attack),
                GetStunDuration(attack),
                attack.finisher));

            if (PlayerJuiceManager.instance != null)
            {
                float styleJuiceBonus = player.playerStyleManager != null ? player.playerStyleManager.RankJuiceBonus : 1f;
                float targetSwitchJuiceBonus = player.playerStyleManager != null && player.playerStyleManager.IsSwitchingTarget(combatTarget)
                    ? player.playerStyleManager.EnemySwitchJuiceGain
                    : 0f;
                PlayerJuiceManager.instance.AddJuice(juiceGain * styleJuiceBonus + targetSwitchJuiceBonus);
            }

            if ((attack.launcher || attack.aerial) && combatTarget != null && combatTarget.IsArmorBroken)
            {
                aerialTarget = combatTarget;
                float followUpWindow = GetAerialFollowUpWindow(attack.aerial);
                aerialFollowUpUntil = Time.time + followUpWindow;
                preserveComboUntil = Mathf.Max(preserveComboUntil, aerialFollowUpUntil);
                PlayerCamera.instance?.AddCombatImpulse(aerialCameraShake, 4f);
            }

            if (attack.aerial && combatTarget != null && combatTarget.IsArmorBroken)
            {
                SuspendAerialTarget(combatTarget);
            }

            player.playerStyleManager?.RegisterHit(moveName, GetStyleValue(attack), attack.finisher, attack.launcher, attack.aerial, attack.dash, isInFlowTime, combatTarget);

            if (PlayerCamera.instance != null)
            {
                float cameraShake = attack.aerial ? aerialCameraShake : attack.finisher ? finisherCameraShake : normalHitCameraShake;
                float fovKick = attack.finisher ? finisherFovKick : attack.aerial ? 3f : 1.5f;
                PlayerCamera.instance.AddCombatImpulse(cameraShake, fovKick);
            }

            if (hitStop > 0f)
            {
                StartCoroutine(ApplyHitStop(hitStop));
            }
        }
    }

    private Vector3 GetHitCenter(ComboAttack attack)
    {
        if (attack.dash)
        {
            CharacterCombatTarget dashTarget = activeDashTarget != null ? activeDashTarget : FindBestDashAttackTarget();
            if (dashTarget != null)
            {
                Vector3 targetPoint = dashTarget.transform.position + Vector3.up;
                float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
                if (distanceToTarget <= attack.hitDistance + dashAttackCatchDistance)
                {
                    return targetPoint;
                }
            }
        }

        if ((attack.aerial || attack.finisher) && aerialTarget != null)
        {
            Vector3 targetPoint = aerialTarget.transform.position + Vector3.up * aerialTargetAimHeight;
            float distanceToTarget = Vector3.Distance(transform.position, targetPoint);
            if (distanceToTarget <= attack.hitDistance + aerialTargetCatchDistance)
            {
                return targetPoint;
            }
        }

        return transform.position + transform.forward * attack.hitDistance + Vector3.up;
    }

    private float GetHitRadius(ComboAttack attack)
    {
        if ((attack.aerial || attack.finisher) && aerialTarget != null)
        {
            return Mathf.Max(attack.hitRadius, aerialTrackedHitRadius);
        }

        return attack.hitRadius;
    }

    private float GetAerialChainBonus(float bonusPerChain)
    {
        if (aerialChainCount <= 1)
            return 0f;

        return (aerialChainCount - 1) * bonusPerChain;
    }

    private bool IsAerialLiftAttack(ComboAttack attack)
    {
        return attack.aerial && attack.type == AttackType.Heavy;
    }

    private float GetLaunchHeight(ComboAttack attack)
    {
        if (IsAerialLiftAttack(attack))
        {
            return attack.launchHeight + GetAerialChainBonus(aerialChainLaunchBonus);
        }

        return attack.launchHeight;
    }

    private void CarryAerialTargetWithPlayer(CharacterCombatTarget assistedTarget, float riseDistance, ComboAttack attack)
    {
        if (assistedTarget == null)
            return;

        Vector3 carryPoint = transform.position
            + transform.forward * aerialCarryForwardOffset
            + Vector3.up * (riseDistance + aerialTargetAimHeight + aerialCarryHeightOffset);
        float holdDuration = aerialHangTime + attack.startupTime + attack.activeTime + 0.08f;
        assistedTarget.CarryToAirComboPoint(carryPoint, aerialRiseDuration, holdDuration);
    }

    private void SuspendAerialTarget(CharacterCombatTarget target)
    {
        float suspendTime = GetAerialSuspendTime();
        if (suspendTime <= 0f)
            return;

        Vector3 suspendPoint = target.transform.position + Vector3.up * aerialSuspendHeight;
        target.CarryToAirComboPoint(suspendPoint, 0.08f, suspendTime);
        float flowWindow = Time.time + GetAerialFollowUpWindow(true);
        aerialFollowUpUntil = Mathf.Max(aerialFollowUpUntil, flowWindow);
        preserveComboUntil = Mathf.Max(preserveComboUntil, flowWindow);
    }

    private string GetMoveName(ComboAttack attack)
    {
        if (!string.IsNullOrEmpty(attack.moveName))
            return attack.moveName;

        if (attack.aerial)
            return "Aerial Follow-Up";
        if (attack.dash)
            return "Dash Attack";
        if (attack.finisher)
            return "Finisher";
        if (attack.launcher)
            return "Launcher";

        return attack.type.ToString();
    }

    private float GetStyleValue(ComboAttack attack)
    {
        if (attack.styleValue > 0f)
            return attack.styleValue;

        float value = attack.damage * 4f;
        if (attack.finisher)
            value += 40f;
        if (attack.dash)
            value += 25f;
        if (attack.launcher)
            value += 30f;
        if (attack.aerial)
            value += 35f;

        return value;
    }

    private float GetArmorDamage(ComboAttack attack)
    {
        if (attack.finisher)
            return finisherArmorDamage;
        if (attack.aerial)
            return aerialArmorDamage;
        if (attack.dash)
            return dashArmorDamage;
        if (attack.type == AttackType.Heavy)
            return heavyArmorDamage;

        return lightArmorDamage;
    }

    private bool RequiresBrokenArmorForStagger(ComboAttack attack)
    {
        return attack.launcher || attack.aerial || attack.finisher || attack.launchHeight > 0f;
    }

    private float GetStunDuration(ComboAttack attack)
    {
        return attack.finisher ? finisherStunDuration : armorBreakStunDuration;
    }

    private float GetAttackRecoveryTime(ComboAttack attack)
    {
        return attack.recoveryTime * GetJuiceRecoveryMultiplier();
    }

    private float GetDodgeCancelRecoveryWindow(float recoveryTime)
    {
        float scaledWindow = dodgeCancelRecoveryWindow * GetJuiceDodgeCancelWindowMultiplier();
        return Mathf.Clamp(scaledWindow, 0f, recoveryTime);
    }

    private float GetAerialFollowUpWindow(bool fromAerialAttack)
    {
        float baseWindow = fromAerialAttack ? aerialChainWindow : aerialFollowUpWindow;
        return baseWindow + GetJuiceAerialWindowBonus();
    }

    private int GetMaxAerialChainCount()
    {
        if (PlayerJuiceManager.instance == null)
            return lowJuiceMaxAerialChainCount;

        switch (PlayerJuiceManager.instance.CurrentTier)
        {
            case PlayerJuiceManager.JuiceTier.Overdrive:
                return overdriveMaxAerialChainCount;
            case PlayerJuiceManager.JuiceTier.High:
                return highJuiceMaxAerialChainCount;
            case PlayerJuiceManager.JuiceTier.Mid:
                return midJuiceMaxAerialChainCount;
            default:
                return lowJuiceMaxAerialChainCount;
        }
    }

    private float GetAerialSuspendTime()
    {
        if (PlayerJuiceManager.instance == null)
            return lowJuiceAerialSuspendTime;

        switch (PlayerJuiceManager.instance.CurrentTier)
        {
            case PlayerJuiceManager.JuiceTier.Overdrive:
                return overdriveAerialSuspendTime;
            case PlayerJuiceManager.JuiceTier.High:
                return highJuiceAerialSuspendTime;
            case PlayerJuiceManager.JuiceTier.Mid:
                return midJuiceAerialSuspendTime;
            default:
                return lowJuiceAerialSuspendTime;
        }
    }

    private float GetDashAttackTargetRange()
    {
        return dashAttackTargetRange * GetJuiceDashRangeMultiplier();
    }

    private float GetAttackLungeDistance(ComboAttack attack)
    {
        if (!attack.dash)
            return attack.lungeDistance;

        return attack.lungeDistance * GetJuiceDashRangeMultiplier();
    }

    private float GetJuiceRecoveryMultiplier()
    {
        return PlayerJuiceManager.instance != null ? PlayerJuiceManager.instance.AttackRecoveryMultiplier : 1f;
    }

    private float GetJuiceAerialWindowBonus()
    {
        return PlayerJuiceManager.instance != null ? PlayerJuiceManager.instance.AerialComboWindowBonus : 0f;
    }

    private float GetJuiceDashRangeMultiplier()
    {
        return PlayerJuiceManager.instance != null ? PlayerJuiceManager.instance.DashAttackRangeMultiplier : 1f;
    }

    private float GetJuiceDodgeCancelWindowMultiplier()
    {
        return PlayerJuiceManager.instance != null ? PlayerJuiceManager.instance.DodgeCancelWindowMultiplier : 1f;
    }

    private IEnumerator ApplyHitStop(float duration)
    {
        hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + duration);

        if (isInHitStop)
            yield break;

        isInHitStop = true;
        RefreshCombatTimeScale();

        while (Time.unscaledTime < hitStopUntil)
        {
            yield return null;
        }

        isInHitStop = false;
        RefreshCombatTimeScale();
    }

    private IEnumerator ApplyFlowTime()
    {
        isInFlowTime = true;
        flowTimeUntil = Time.unscaledTime + flowTimeDuration;
        RefreshCombatTimeScale();

        while (Time.unscaledTime < flowTimeUntil)
        {
            yield return null;
        }

        isInFlowTime = false;
        flowTimeUntil = 0f;
        RefreshCombatTimeScale();
    }

    private void RefreshCombatTimeScale()
    {
        float targetTimeScale = 1f;
        if (isInFlowTime)
        {
            targetTimeScale = flowTimeScale;
        }

        if (isInHitStop)
        {
            targetTimeScale = 0.05f;
        }

        Time.timeScale = targetTimeScale;
        Time.fixedDeltaTime = defaultFixedDeltaTime * targetTimeScale;
    }

    private void ResetCombatTimeScale()
    {
        isInHitStop = false;
        isInFlowTime = false;
        flowTimeUntil = 0f;
        hitStopUntil = 0f;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = defaultFixedDeltaTime;
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

    private void TryQueueDashAttack()
    {
        if (isDodging || Time.time <= dashAttackUntil)
        {
            dashAttackQueued = true;
        }
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
