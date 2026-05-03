using System;
using UnityEngine;

public class PlayerInputManager : CharacterInputManager
{
    public PlayerInputManager instance;
    
    private PlayerControls playerControls;
    
    [Header("Player Movement Inputs")]
    private Vector2 movementInput;
    public float verticalInput;
    public float horizontalInput;
    public float moveAmount;

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

    private void OnEnable()
    {
        playerControls = new PlayerControls();
        
        playerControls.Player.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
    }

    private void Update()
    {
        HandleAllInputs();
    }

    private void HandleAllInputs()
    {
        HandlePlayerMovementInput();
        HandleCameraMovementInput();
    }

    private void HandlePlayerMovementInput()
    {
        verticalInput = movementInput.y;
        horizontalInput = movementInput.x;
        
        moveAmount = Mathf.Clamp01(Mathf.Abs(verticalInput) + Mathf.Abs(horizontalInput));
    }

    private void HandleCameraMovementInput()
    {
        //camera input stuff
    }
}
