using System.Collections.Generic;
using UnityEngine;

public class PlayerStyleManager : MonoBehaviour
{
    public enum StyleRank
    {
        Dry,
        Buzzing,
        Charged,
        Voltage,
        Overclocked,
        Surge,
        LimitBreak
    }

    [Header("Style")]
    [SerializeField] private float currentStyle;
    [SerializeField] private float maxStyle = 1000f;
    [SerializeField] private float decayDelay = 2.5f;
    [SerializeField] private float decayRate = 60f;
    [SerializeField] private float repeatedMovePenalty = 0.55f;
    [SerializeField] private int recentMoveHistorySize = 4;
    [SerializeField] private float rankJuiceMultiplier = 0.12f;

    [Header("Movement Flow Rewards")]
    [SerializeField] private float dashAttackMultiplier = 1.18f;
    [SerializeField] private float airAttackMultiplier = 1.28f;
    [SerializeField] private float enemySwitchStyleGain = 45f;
    [SerializeField] private float enemySwitchJuiceGain = 5f;
    [SerializeField] private float perfectDodgeStyleGain = 90f;
    [SerializeField] private float aggressionStylePerSecond = 10f;
    [SerializeField] private float aggressionWindow = 2.4f;
    [SerializeField] private float aggressionMovementThreshold = 0.25f;
    [SerializeField] private float aggressionTickInterval = 0.25f;

    [Header("Style Penalties")]
    [SerializeField] private float repeatedMoveStyleLoss = 14f;
    [SerializeField] private float standingStillPenaltyDelay = 1.1f;
    [SerializeField] private float standingStillPenaltyPerSecond = 28f;
    [SerializeField] private float standingStillPenaltyTickInterval = 0.25f;
    [SerializeField] private float damageTakenStyleMultiplier = 0.5f;
    [SerializeField] private float damageTakenStyleLoss = 90f;

    [Header("Runtime")]
    [SerializeField] private StyleRank currentRank;
    [SerializeField] private int hitStreak;
    [SerializeField] private string lastMoveName;
    [SerializeField] private string recentMoveHistory;
    [SerializeField] private Object lastHitTarget;
    [SerializeField] private float aggressionUntil;

    private readonly Queue<string> recentMoveNames = new Queue<string>();
    private PlayerManager player;
    private float lastStyleGainTime;
    private float idleStartedAt;
    private float lastAggressionTickTime;
    private float lastStandingPenaltyTime;

    public float CurrentStyle => currentStyle;
    public StyleRank CurrentRank => currentRank;
    public string CurrentRankName => GetRankName(currentRank);
    public int HitStreak => hitStreak;
    public float RankJuiceBonus => 1f + ((int)currentRank * rankJuiceMultiplier);
    public float EnemySwitchJuiceGain => enemySwitchJuiceGain;
    public float GetMoveRepetitionMultiplier(string moveName) => CalculateMoveRepetitionMultiplier(moveName);
    public bool IsSwitchingTarget(Object hitTarget) => hitTarget != null && lastHitTarget != null && hitTarget != lastHitTarget;

    private void Awake()
    {
        player = GetComponent<PlayerManager>();
    }

    private void Update()
    {
        UpdateMovementFlow();
        UpdateStyleDecay();
    }

    public void RegisterHit(string moveName, float baseStyle, bool finisher, bool launched, bool aerial, bool dash, bool witchTime, Object hitTarget)
    {
        float gain = baseStyle;
        int repetitionCount = CountRecentMoveUses(moveName);
        bool repeatedMove = repetitionCount > 0;

        if (repeatedMove)
        {
            gain *= CalculateMoveRepetitionMultiplier(moveName);
            ApplyStyleLoss(repeatedMoveStyleLoss * repetitionCount);
        }

        if (finisher)
        {
            gain *= 1.45f;
        }

        if (launched)
        {
            gain *= 1.3f;
        }

        if (aerial)
        {
            gain *= airAttackMultiplier;
        }

        if (dash)
        {
            gain *= dashAttackMultiplier;
        }

        if (witchTime)
        {
            gain *= 1.6f;
        }

        if (IsSwitchingTarget(hitTarget))
        {
            gain += enemySwitchStyleGain;
        }

        hitStreak++;
        AddStyle(gain + hitStreak * 3f);
        lastMoveName = moveName;
        AddMoveToHistory(moveName);
        lastHitTarget = hitTarget;
        ExtendAggression();
    }

    public void RegisterPerfectDodge()
    {
        hitStreak++;
        AddStyle(perfectDodgeStyleGain + hitStreak * 2f);
        lastMoveName = "Perfect Dodge";
        ExtendAggression();
    }

    public void RegisterDamageTaken()
    {
        hitStreak = 0;
        currentStyle = Mathf.Max(0f, currentStyle * damageTakenStyleMultiplier - damageTakenStyleLoss);
        currentRank = CalculateRank(currentStyle);
        lastMoveName = string.Empty;
        ClearMoveHistory();
        lastHitTarget = null;
        aggressionUntil = 0f;
    }

    public void BreakStyle()
    {
        RegisterDamageTaken();
    }

    private void UpdateMovementFlow()
    {
        bool inAggression = Time.time <= aggressionUntil;
        bool isMoving = PlayerInputManager.instance != null && PlayerInputManager.instance.moveAmount >= aggressionMovementThreshold;
        bool isLockedInAction = player != null && player.isPerformingAction;

        if (inAggression && isMoving)
        {
            idleStartedAt = 0f;

            if (Time.time - lastAggressionTickTime >= aggressionTickInterval)
            {
                AddStyle(aggressionStylePerSecond * aggressionTickInterval);
                lastAggressionTickTime = Time.time;
            }

            return;
        }

        if (!inAggression || isLockedInAction)
        {
            idleStartedAt = 0f;
            return;
        }

        if (idleStartedAt <= 0f)
        {
            idleStartedAt = Time.time;
            lastStandingPenaltyTime = Time.time;
            return;
        }

        if (Time.time - idleStartedAt < standingStillPenaltyDelay)
            return;

        if (Time.time - lastStandingPenaltyTime < standingStillPenaltyTickInterval)
            return;

        ApplyStyleLoss(standingStillPenaltyPerSecond * standingStillPenaltyTickInterval);
        lastStandingPenaltyTime = Time.time;
    }

    private void UpdateStyleDecay()
    {
        if (Time.time - lastStyleGainTime < decayDelay)
            return;

        if (currentStyle <= 0f)
            return;

        currentStyle = Mathf.Max(0f, currentStyle - decayRate * Time.deltaTime);
        currentRank = CalculateRank(currentStyle);

        if (currentStyle <= 0f)
        {
            hitStreak = 0;
            lastMoveName = string.Empty;
            ClearMoveHistory();
            lastHitTarget = null;
        }
    }

    private void AddStyle(float amount)
    {
        float styleGainMultiplier = PlayerJuiceManager.instance != null ? PlayerJuiceManager.instance.StyleGainMultiplier : 1f;
        currentStyle = Mathf.Clamp(currentStyle + amount * styleGainMultiplier, 0f, maxStyle);
        currentRank = CalculateRank(currentStyle);
        lastStyleGainTime = Time.time;
    }

    private void ApplyStyleLoss(float amount)
    {
        currentStyle = Mathf.Max(0f, currentStyle - amount);
        currentRank = CalculateRank(currentStyle);

        if (currentStyle <= 0f)
        {
            hitStreak = 0;
            lastMoveName = string.Empty;
            ClearMoveHistory();
            lastHitTarget = null;
        }
    }

    private void ExtendAggression()
    {
        aggressionUntil = Time.time + aggressionWindow;
    }

    private float CalculateMoveRepetitionMultiplier(string moveName)
    {
        int repetitionCount = CountRecentMoveUses(moveName);
        if (repetitionCount <= 0)
            return 1f;

        return Mathf.Pow(Mathf.Clamp01(repeatedMovePenalty), repetitionCount);
    }

    private int CountRecentMoveUses(string moveName)
    {
        if (string.IsNullOrEmpty(moveName))
            return 0;

        int count = 0;
        foreach (string recentMoveName in recentMoveNames)
        {
            if (recentMoveName == moveName)
            {
                count++;
            }
        }

        return count;
    }

    private void AddMoveToHistory(string moveName)
    {
        if (string.IsNullOrEmpty(moveName))
            return;

        recentMoveNames.Enqueue(moveName);
        while (recentMoveNames.Count > Mathf.Max(1, recentMoveHistorySize))
        {
            recentMoveNames.Dequeue();
        }

        recentMoveHistory = string.Join(", ", recentMoveNames);
    }

    private void ClearMoveHistory()
    {
        recentMoveNames.Clear();
        recentMoveHistory = string.Empty;
    }

    private StyleRank CalculateRank(float style)
    {
        if (style >= 900f)
            return StyleRank.LimitBreak;
        if (style >= 750f)
            return StyleRank.Surge;
        if (style >= 600f)
            return StyleRank.Overclocked;
        if (style >= 430f)
            return StyleRank.Voltage;
        if (style >= 280f)
            return StyleRank.Charged;
        if (style >= 140f)
            return StyleRank.Buzzing;

        return StyleRank.Dry;
    }

    private string GetRankName(StyleRank rank)
    {
        if (rank == StyleRank.LimitBreak)
            return "Limit Break";

        return rank.ToString();
    }
}
