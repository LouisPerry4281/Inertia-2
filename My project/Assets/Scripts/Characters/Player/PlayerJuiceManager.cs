using System;
using System.Collections;
using UnityEngine;

public class PlayerJuiceManager : MonoBehaviour
{
    public enum JuiceTier
    {
        Low,
        Mid,
        High,
        Overdrive
    }

    public static PlayerJuiceManager instance;

    [Header("Juice Amounts")]
    public float currentJuice = 50;
    [SerializeField] private float maxJuice = 100;

    [Header("Juice Tiers")]
    [SerializeField] private float midTierThreshold = 0.35f;
    [SerializeField] private float highTierThreshold = 0.7f;
    [SerializeField] private float overdriveActivationThreshold = 0.999f;
    [SerializeField] private float overdriveExitThreshold = 0.7f;
    [SerializeField] private float midRecoveryMultiplier = 0.94f;
    [SerializeField] private float highRecoveryMultiplier = 0.86f;
    [SerializeField] private float overdriveRecoveryMultiplier = 0.72f;
    [SerializeField] private float midAerialWindowBonus = 0.12f;
    [SerializeField] private float highAerialWindowBonus = 0.28f;
    [SerializeField] private float overdriveAerialWindowBonus = 0.5f;
    [SerializeField] private float midDashRangeMultiplier = 1.08f;
    [SerializeField] private float highDashRangeMultiplier = 1.2f;
    [SerializeField] private float overdriveDashRangeMultiplier = 1.35f;
    [SerializeField] private float midDodgeCancelWindowMultiplier = 1.1f;
    [SerializeField] private float highDodgeCancelWindowMultiplier = 1.28f;
    [SerializeField] private float overdriveDodgeCancelWindowMultiplier = 1.55f;
    [SerializeField] private float overdriveStyleGainMultiplier = 1.3f;

    [Header("Overdrive")]
    [SerializeField] private float overdriveJuiceDrainRate = 18f;
    [SerializeField] private bool isInOverdrive;
    
    [Header("Juice Decay")]
    [SerializeField] private float juiceDecayRate = 0.8f;
    [SerializeField] private float juiceDecayDelay = 3f;
    private float juiceDecayTimer = 0f;
    bool isDecaying = false;

    public float MaxJuice => maxJuice;
    public float JuiceNormalized => maxJuice <= 0f ? 0f : Mathf.Clamp01(currentJuice / maxJuice);
    public JuiceTier CurrentTier => CalculateTier(JuiceNormalized);
    public bool IsInOverdrive => isInOverdrive;
    public float AttackRecoveryMultiplier => GetTierValue(1f, midRecoveryMultiplier, highRecoveryMultiplier, overdriveRecoveryMultiplier);
    public float AerialComboWindowBonus => GetTierValue(0f, midAerialWindowBonus, highAerialWindowBonus, overdriveAerialWindowBonus);
    public float DashAttackRangeMultiplier => GetTierValue(1f, midDashRangeMultiplier, highDashRangeMultiplier, overdriveDashRangeMultiplier);
    public float DodgeCancelWindowMultiplier => GetTierValue(1f, midDodgeCancelWindowMultiplier, highDodgeCancelWindowMultiplier, overdriveDodgeCancelWindowMultiplier);
    public float StyleGainMultiplier => isInOverdrive ? overdriveStyleGainMultiplier : 1f;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        UpdateOverdrive();
        if (isInOverdrive)
            return;

        CheckJuiceDecay();
        JuiceDecay();
    }

    public void AddJuice(float juiceToAdd)
    {
        ResetJuiceTimer();
        
        currentJuice += juiceToAdd;

        if (currentJuice > maxJuice)
        {
            currentJuice = maxJuice;
        }

        if (JuiceNormalized >= overdriveActivationThreshold)
        {
            isInOverdrive = true;
            currentJuice = maxJuice;
        }
        
        //Update UI Stuff
    }

    public void RemoveJuice(float juiceToRemove)
    {
        currentJuice -= juiceToRemove;

        if (currentJuice < 0)
        {
            currentJuice = 0;
        }

        if (isInOverdrive && JuiceNormalized <= overdriveExitThreshold)
        {
            isInOverdrive = false;
        }
        
        //Update UI Stuff
    }

    private void UpdateOverdrive()
    {
        if (!isInOverdrive)
        {
            if (JuiceNormalized >= overdriveActivationThreshold)
            {
                isInOverdrive = true;
            }

            return;
        }

        RemoveJuice(overdriveJuiceDrainRate * Time.deltaTime);
    }

    private void CheckJuiceDecay()
    {
        juiceDecayTimer += Time.deltaTime;

        if (juiceDecayTimer >= juiceDecayDelay)
        {
            isDecaying = true;
        }
        else
        {
            isDecaying = false;
        }
    }

    private void JuiceDecay()
    {
        if (!isDecaying)
            return;

        RemoveJuice(juiceDecayRate * Time.deltaTime);
    }

    public void ResetJuiceTimer()
    {
        juiceDecayTimer = 0f;
    }

    private JuiceTier CalculateTier(float normalizedJuice)
    {
        if (isInOverdrive)
            return JuiceTier.Overdrive;
        if (normalizedJuice >= highTierThreshold)
            return JuiceTier.High;
        if (normalizedJuice >= midTierThreshold)
            return JuiceTier.Mid;

        return JuiceTier.Low;
    }

    private float GetTierValue(float lowValue, float midValue, float highValue, float overdriveValue)
    {
        switch (CurrentTier)
        {
            case JuiceTier.Overdrive:
                return overdriveValue;
            case JuiceTier.High:
                return highValue;
            case JuiceTier.Mid:
                return midValue;
            default:
                return lowValue;
        }
    }
}
