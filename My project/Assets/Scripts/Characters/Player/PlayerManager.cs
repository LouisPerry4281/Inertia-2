using UnityEngine;

public class PlayerManager : CharacterManager
{
    [HideInInspector] public PlayerLocomotion playerLocomotion;

    protected override void Awake()
    {
        base.Awake();
        
        playerLocomotion = GetComponent<PlayerLocomotion>();
    }

    private void Update()
    {
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
