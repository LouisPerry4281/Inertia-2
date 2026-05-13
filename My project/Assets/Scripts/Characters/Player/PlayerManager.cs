using UnityEngine;

public class PlayerManager : CharacterManager
{
    [HideInInspector] public PlayerLocomotion playerLocomotion;
    [HideInInspector] public PlayerCombatManager playerCombatManager;
    [HideInInspector] public PlayerDamageReceiver playerDamageReceiver;

    protected override void Awake()
    {
        base.Awake();
        
        playerLocomotion = GetComponent<PlayerLocomotion>();
        playerCombatManager = GetComponent<PlayerCombatManager>();
        if (playerCombatManager == null)
        {
            playerCombatManager = gameObject.AddComponent<PlayerCombatManager>();
        }

        playerDamageReceiver = GetComponent<PlayerDamageReceiver>();
        if (playerDamageReceiver == null)
        {
            playerDamageReceiver = gameObject.AddComponent<PlayerDamageReceiver>();
        }
    }

    private void Update()
    {
        playerCombatManager?.HandleAllCombat();
        playerLocomotion.HandleAllMovement();
    }

    private void LateUpdate()
    {
        if (PlayerCamera.instance != null)
        {
            PlayerCamera.instance.HandleAllCameraActions();
        }
    }
}
