using UnityEngine;
using System.Collections;

public class CharacterCombatTarget : MonoBehaviour, IDamageable
{
    public enum EnemyCombatState
    {
        Normal,
        Staggered,
        Launched,
        WallSplatted,
        Vulnerable
    }

    [SerializeField] private float maxHealth = 100f;
    [SerializeField] private bool destroyOnDeath;
    [SerializeField] private Renderer[] hitFlashRenderers;
    [SerializeField] private Color hitFlashColor = Color.white;
    [SerializeField] private float hitFlashTime = 0.07f;

    [Header("Armor")]
    [SerializeField] private float maxArmor = 60f;
    [SerializeField] private float armorRegenDelay = 3f;
    [SerializeField] private float armorRegenRate = 12f;
    [SerializeField] private float armorBrokenDuration = 2.5f;
    [SerializeField] private float armorBreakDamageMultiplier = 1.15f;
    [SerializeField] private Color armorBreakFlashColor = new Color(0.2f, 0.9f, 1f, 1f);

    [Header("Launch")]
    [SerializeField] private float launchGravity = -22f;
    [SerializeField] private float launchHangTime = 0.45f;
    [SerializeField] private float launchHangGravityScale = 0.15f;
    [SerializeField] private float launchHorizontalDamping = 5f;
    [SerializeField] private float maxLaunchAirTime = 1.8f;
    [SerializeField] private float groundedProbeDistance = 0.18f;
    [SerializeField] private LayerMask groundLayers = ~0;

    [Header("Air Combo")]
    [SerializeField] private float airComboCarrySpeed = 22f;

    [Header("Combat State")]
    [SerializeField] private EnemyCombatState currentState = EnemyCombatState.Normal;

    private float currentHealth;
    private float currentArmor;
    private float armorRegenBlockedUntil;
    private float armorBrokenUntil;
    private float stateExpiresAt;
    private Rigidbody targetRigidbody;
    private CharacterController targetCharacterController;
    private EnemyCombatAI enemyCombatAI;
    private MaterialPropertyBlock propertyBlock;
    private Coroutine hitFlashRoutine;
    private Coroutine launchRoutine;
    private Coroutine airComboCarryRoutine;
    private Coroutine stunRoutine;
    private Color defaultColor = Color.white;
    private bool enemyAIWasEnabled;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public EnemyCombatState CurrentState => currentState;
    public bool IsNormal => currentState == EnemyCombatState.Normal;
    public bool IsStaggered => currentState == EnemyCombatState.Staggered;
    public bool IsLaunched => currentState == EnemyCombatState.Launched;
    public bool IsWallSplatted => currentState == EnemyCombatState.WallSplatted;
    public bool IsVulnerable => currentState == EnemyCombatState.Vulnerable;

    private void Awake()
    {
        currentHealth = maxHealth;
        currentArmor = maxArmor;
        targetRigidbody = GetComponent<Rigidbody>();
        targetCharacterController = GetComponent<CharacterController>();
        enemyCombatAI = GetComponent<EnemyCombatAI>();
        propertyBlock = new MaterialPropertyBlock();

        if (hitFlashRenderers == null || hitFlashRenderers.Length == 0)
        {
            hitFlashRenderers = GetComponentsInChildren<Renderer>();
        }

        if (hitFlashRenderers.Length > 0 && hitFlashRenderers[0].sharedMaterial != null)
        {
            defaultColor = hitFlashRenderers[0].sharedMaterial.HasProperty(BaseColorId)
                ? hitFlashRenderers[0].sharedMaterial.GetColor(BaseColorId)
                : hitFlashRenderers[0].sharedMaterial.color;
        }
    }

    public void TakeDamage(DamageInfo damageInfo)
    {
        bool armorWasBroken = IsArmorBroken;
        bool armorJustBroke = ApplyArmorDamage(damageInfo.armorDamage);
        bool canStagger = !damageInfo.requiresBrokenArmorForStagger || IsArmorBroken;
        float finalDamage = IsArmorBroken ? damageInfo.damage * armorBreakDamageMultiplier : damageInfo.damage;

        currentHealth = Mathf.Max(0f, currentHealth - finalDamage);

        Vector3 allowedLaunch = canStagger ? damageInfo.launch : Vector3.zero;
        Vector3 allowedForce = canStagger ? damageInfo.force : damageInfo.force * 0.35f;
        Vector3 totalForce = allowedForce + allowedLaunch;
        bool launched = false;
        if (targetRigidbody != null && !targetRigidbody.isKinematic && totalForce.sqrMagnitude > 0f)
        {
            targetRigidbody.AddForceAtPosition(totalForce, damageInfo.hitPoint, ForceMode.Impulse);
            launched = allowedLaunch.sqrMagnitude > 0f;
        }
        else if (allowedLaunch.sqrMagnitude > 0f)
        {
            StartLaunch(totalForce);
            launched = true;
        }
        else if (canStagger && damageInfo.stunDuration > 0f)
        {
            StartStun(damageInfo.stunDuration);
        }

        if (!launched && !armorWasBroken && armorJustBroke && damageInfo.stunDuration > 0f)
        {
            StartStun(damageInfo.stunDuration);
        }

        if (hitFlashRoutine != null)
        {
            StopCoroutine(hitFlashRoutine);
        }
        hitFlashRoutine = StartCoroutine(FlashOnHit(armorJustBroke ? armorBreakFlashColor : hitFlashColor));

        if (destroyOnDeath && currentHealth <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateCombatState();
        RegenerateArmor();
    }

    public bool IsArmorBroken => IsVulnerable || Time.time < armorBrokenUntil;

    public void EnterStaggered(float duration)
    {
        EnterState(EnemyCombatState.Staggered, duration);
    }

    public void EnterLaunched(float duration)
    {
        EnterState(EnemyCombatState.Launched, duration);
    }

    public void EnterWallSplatted(float duration)
    {
        EnterState(EnemyCombatState.WallSplatted, duration);
    }

    public void EnterVulnerable(float duration)
    {
        armorBrokenUntil = Mathf.Max(armorBrokenUntil, Time.time + duration);
        EnterState(EnemyCombatState.Vulnerable, duration);
    }

    public void ExitCombatState(EnemyCombatState state)
    {
        if (currentState != state)
            return;

        ExitToBestAvailableState();
    }

    public void ExitCurrentState()
    {
        ExitToBestAvailableState();
    }

    private void EnterState(EnemyCombatState state, float duration)
    {
        currentState = state;
        stateExpiresAt = duration > 0f ? Time.time + duration : 0f;
    }

    private void UpdateCombatState()
    {
        if (currentState == EnemyCombatState.Normal)
            return;

        if (currentState == EnemyCombatState.Vulnerable && Time.time < armorBrokenUntil)
            return;

        if (stateExpiresAt > 0f && Time.time < stateExpiresAt)
            return;

        ExitToBestAvailableState();
    }

    private void ExitToBestAvailableState()
    {
        if (Time.time < armorBrokenUntil)
        {
            currentState = EnemyCombatState.Vulnerable;
            stateExpiresAt = armorBrokenUntil;
            return;
        }

        currentState = EnemyCombatState.Normal;
        stateExpiresAt = 0f;
    }

    private bool ApplyArmorDamage(float armorDamage)
    {
        if (maxArmor <= 0f || armorDamage <= 0f)
            return false;

        armorRegenBlockedUntil = Time.time + armorRegenDelay;

        if (IsArmorBroken)
        {
            armorBrokenUntil = Mathf.Max(armorBrokenUntil, Time.time + armorBrokenDuration * 0.35f);
            return false;
        }

        currentArmor = Mathf.Max(0f, currentArmor - armorDamage);
        if (currentArmor > 0f)
            return false;

        BreakArmor();
        return true;
    }

    private void BreakArmor()
    {
        currentArmor = 0f;
        EnterVulnerable(armorBrokenDuration);
    }

    private void RegenerateArmor()
    {
        if (maxArmor <= 0f || IsArmorBroken || Time.time < armorRegenBlockedUntil)
            return;

        if (currentArmor >= maxArmor)
            return;

        currentArmor = Mathf.Min(maxArmor, currentArmor + armorRegenRate * Time.deltaTime);
    }

    private void StartLaunch(Vector3 launchVelocity)
    {
        if (launchRoutine != null)
        {
            StopCoroutine(launchRoutine);
            RestoreEnemyAI();
        }

        if (airComboCarryRoutine != null)
        {
            StopCoroutine(airComboCarryRoutine);
            RestoreEnemyAI();
            airComboCarryRoutine = null;
        }

        EnterLaunched(maxLaunchAirTime);
        launchRoutine = StartCoroutine(SimulateLaunch(launchVelocity));
    }

    public void CarryToAirComboPoint(Vector3 targetPoint, float moveDuration, float holdDuration)
    {
        if (!IsArmorBroken)
            return;

        if (moveDuration <= 0f)
        {
            MoveLaunchedTarget(targetPoint - transform.position);
            return;
        }

        if (launchRoutine != null)
        {
            StopCoroutine(launchRoutine);
            RestoreEnemyAI();
            launchRoutine = null;
        }

        if (airComboCarryRoutine != null)
        {
            StopCoroutine(airComboCarryRoutine);
            RestoreEnemyAI();
        }

        EnterLaunched(moveDuration + holdDuration);
        airComboCarryRoutine = StartCoroutine(CarryToPoint(targetPoint, moveDuration, holdDuration));
    }

    private IEnumerator CarryToPoint(Vector3 targetPoint, float moveDuration, float holdDuration)
    {
        enemyAIWasEnabled = enemyCombatAI != null && enemyCombatAI.enabled;
        if (enemyCombatAI != null)
        {
            enemyCombatAI.enabled = false;
        }

        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            float delta = Time.deltaTime;
            elapsed += delta;

            Vector3 displacement = targetPoint - transform.position;
            float maxStep = Mathf.Max(airComboCarrySpeed * delta, displacement.magnitude * Mathf.Clamp01(delta / Mathf.Max(moveDuration - elapsed + delta, 0.001f)));
            MoveLaunchedTarget(Vector3.ClampMagnitude(displacement, maxStep));
            yield return null;
        }

        float holdTimer = holdDuration;
        while (holdTimer > 0f)
        {
            float delta = Time.deltaTime;
            holdTimer -= delta;

            Vector3 displacement = targetPoint - transform.position;
            MoveLaunchedTarget(Vector3.ClampMagnitude(displacement, airComboCarrySpeed * delta));
            yield return null;
        }

        RestoreEnemyAI();
        ExitCombatState(EnemyCombatState.Launched);
        airComboCarryRoutine = null;
    }

    private void StartStun(float duration)
    {
        if (duration <= 0f)
            return;

        if (stunRoutine != null)
        {
            StopCoroutine(stunRoutine);
            RestoreEnemyAI();
        }

        EnterStaggered(duration);
        stunRoutine = StartCoroutine(StunForDuration(duration));
    }

    private IEnumerator StunForDuration(float duration)
    {
        enemyAIWasEnabled = enemyCombatAI != null && enemyCombatAI.enabled;
        if (enemyCombatAI != null)
        {
            enemyCombatAI.enabled = false;
        }

        yield return new WaitForSecondsRealtime(duration);

        RestoreEnemyAI();
        ExitCombatState(EnemyCombatState.Staggered);
        stunRoutine = null;
    }

    private IEnumerator SimulateLaunch(Vector3 launchVelocity)
    {
        enemyAIWasEnabled = enemyCombatAI != null && enemyCombatAI.enabled;
        if (enemyCombatAI != null)
        {
            enemyCombatAI.enabled = false;
        }

        Vector3 velocity = launchVelocity;
        float elapsed = 0f;
        float hangTimer = launchHangTime;
        bool hasReachedApex = false;

        while (elapsed < maxLaunchAirTime)
        {
            float delta = Time.deltaTime;
            elapsed += delta;

            if (!hasReachedApex && velocity.y <= 0f)
            {
                hasReachedApex = true;
            }

            if (hasReachedApex && hangTimer > 0f)
            {
                hangTimer -= delta;
                velocity.y = Mathf.Min(velocity.y, 0f);
                velocity.y += launchGravity * launchHangGravityScale * delta;
            }
            else
            {
                velocity.y += launchGravity * delta;
            }

            Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, launchHorizontalDamping * delta);
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;

            MoveLaunchedTarget(velocity * delta);

            if (elapsed > 0.12f && velocity.y <= 0f && IsGrounded())
            {
                break;
            }

            yield return null;
        }

        RestoreEnemyAI();
        ExitCombatState(EnemyCombatState.Launched);
        launchRoutine = null;
    }

    private void MoveLaunchedTarget(Vector3 displacement)
    {
        if (targetCharacterController != null && targetCharacterController.enabled)
        {
            targetCharacterController.Move(displacement);
            return;
        }

        transform.position += displacement;
    }

    private bool IsGrounded()
    {
        if (targetCharacterController != null && targetCharacterController.enabled)
            return targetCharacterController.isGrounded;

        return Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, groundedProbeDistance + 0.1f, groundLayers, QueryTriggerInteraction.Ignore);
    }

    private void RestoreEnemyAI()
    {
        if (enemyCombatAI != null)
        {
            enemyCombatAI.enabled = enemyAIWasEnabled;
        }
    }

    private IEnumerator FlashOnHit(Color flashColor)
    {
        SetRenderColor(flashColor);
        yield return new WaitForSecondsRealtime(hitFlashTime);
        SetRenderColor(defaultColor);
        hitFlashRoutine = null;
    }

    private void SetRenderColor(Color color)
    {
        foreach (Renderer hitFlashRenderer in hitFlashRenderers)
        {
            if (hitFlashRenderer == null)
                continue;

            hitFlashRenderer.GetPropertyBlock(propertyBlock);
            if (hitFlashRenderer.sharedMaterial != null && hitFlashRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                propertyBlock.SetColor(BaseColorId, color);
            }
            else
            {
                propertyBlock.SetColor(ColorId, color);
            }

            hitFlashRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
