using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputManager : CharacterInputManager
{
    public static PlayerInputManager instance;
    
    public PlayerManager player;
    
    private PlayerControls playerControls;
    
    [Header("Camera Movement Input")]
    [SerializeField] private Vector2 cameraInput;
    public float cameraVerticalInput;
    public float cameraHorizontalInput;
    
    [Header("Player Movement Inputs")]
    private Vector2 movementInput;
    public float verticalInput;
    public float horizontalInput;
    public float moveAmount;

    [Header("Combat Inputs")]
    public bool lightAttackInput;
    public bool heavyAttackInput;
    public bool dodgeInput;

    private InputAction lightAttackAction;
    private InputAction heavyAttackAction;
    private InputAction dodgeAction;

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
        if (playerControls == null)
        {
            playerControls = new PlayerControls();

            playerControls.PlayerMovement.Movement.performed += i => movementInput = i.ReadValue<Vector2>();
            playerControls.PlayerMovement.Movement.canceled += i => movementInput = Vector2.zero;

            playerControls.PlayerCamera.Movement.performed += i => cameraInput = i.ReadValue<Vector2>();
            playerControls.PlayerCamera.Movement.canceled += i => cameraInput = Vector2.zero;
            playerControls.PlayerCamera.Mouse.performed += i => cameraInput = i.ReadValue<Vector2>();
            playerControls.PlayerCamera.Mouse.canceled += i => cameraInput = Vector2.zero;

            CreateCombatActions();
        }

        playerControls.Enable();
        lightAttackAction.Enable();
        heavyAttackAction.Enable();
        dodgeAction.Enable();
    }

    private void OnDisable()
    {
        playerControls?.Disable();
        lightAttackAction?.Disable();
        heavyAttackAction?.Disable();
        dodgeAction?.Disable();
    }

    private void OnDestroy()
    {
        playerControls?.Dispose();
        lightAttackAction?.Dispose();
        heavyAttackAction?.Dispose();
        dodgeAction?.Dispose();
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

    public bool ConsumeLightAttackInput()
    {
        if (!lightAttackInput)
            return false;

        lightAttackInput = false;
        return true;
    }

    public bool ConsumeHeavyAttackInput()
    {
        if (!heavyAttackInput)
            return false;

        heavyAttackInput = false;
        return true;
    }

    public bool ConsumeDodgeInput()
    {
        if (!dodgeInput)
            return false;

        dodgeInput = false;
        return true;
    }

    private void CreateCombatActions()
    {
        lightAttackAction = new InputAction("Light Attack", InputActionType.Button);
        lightAttackAction.AddBinding("<Mouse>/leftButton");
        lightAttackAction.AddBinding("<Gamepad>/buttonWest");
        lightAttackAction.performed += _ => lightAttackInput = true;

        heavyAttackAction = new InputAction("Heavy Attack", InputActionType.Button);
        heavyAttackAction.AddBinding("<Mouse>/rightButton");
        heavyAttackAction.AddBinding("<Gamepad>/buttonNorth");
        heavyAttackAction.performed += _ => heavyAttackInput = true;

        dodgeAction = new InputAction("Dodge", InputActionType.Button);
        dodgeAction.AddBinding("<Keyboard>/space");
        dodgeAction.AddBinding("<Gamepad>/buttonEast");
        dodgeAction.performed += _ => dodgeInput = true;
    }

    private void HandlePlayerMovementInput()
    {
        verticalInput = movementInput.y;
        horizontalInput = movementInput.x;
        
        moveAmount = Mathf.Clamp01(Mathf.Abs(verticalInput) + Mathf.Abs(horizontalInput));
    }

    private void HandleCameraMovementInput()
    {
        cameraVerticalInput = cameraInput.y;
        cameraHorizontalInput = cameraInput.x;
    }
}
