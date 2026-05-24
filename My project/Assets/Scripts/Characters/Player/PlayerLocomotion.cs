using UnityEngine;

public class PlayerLocomotion : CharacterLocomotion
{
    private PlayerManager player;
    
    public float verticalMovement;
    public float horizontalMovement;
    public float moveAmount;
    private float finalMovementSpeed;
    
    [Header("Movement Settings")]
    private Vector3 moveDirection;
    private Vector3 targetRotationDirection;
    [SerializeField] private float baseMovementSpeed = 5;
    [SerializeField] private float maxJuiceMovementBonus = 2.2f;
    [SerializeField] private float acceleration = 30f;
    [SerializeField] private float rotationSpeed = 15;
    [SerializeField] private float gravity = -24f;
    [SerializeField] private float groundedVerticalVelocity = -2f;
    private Vector3 currentVelocity;
    private Vector3 verticalVelocity;
    
    private void Awake()
    {
        player = GetComponent<PlayerManager>();
    }

    public void HandleAllMovement()
    {
        if (player.isPerformingAction)
            return;

        HandleGravity();
        HandleGroundMovement();
        HandleRotation();
    }

    public void ResetVerticalVelocity()
    {
        verticalVelocity = Vector3.zero;
    }
    
    private void GetMovementValues()
    {
        verticalMovement = PlayerInputManager.instance.verticalInput;
        horizontalMovement = PlayerInputManager.instance.horizontalInput;
        moveAmount = PlayerInputManager.instance.moveAmount;
    }

    private void HandleGroundMovement()
    {
        if (!player.canMove)
            return;

        GetMovementValues();

        //Movement direction is based on camera perspective + inputs
        moveDirection = PlayerCamera.instance.transform.forward * verticalMovement;
        moveDirection = moveDirection + PlayerCamera.instance.transform.right * horizontalMovement;
        moveDirection.Normalize();
        moveDirection.y = 0;
        
        finalMovementSpeed = ApplyJuiceToSpeed(baseMovementSpeed);
        Vector3 targetVelocity = moveDirection * finalMovementSpeed;
        currentVelocity = Vector3.MoveTowards(currentVelocity, targetVelocity, acceleration * Time.deltaTime);
        
        player.characterController.Move(currentVelocity * Time.deltaTime);
    }

    private void HandleGravity()
    {
        if (player.characterController == null)
            return;

        if (player.characterController.isGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedVerticalVelocity;
        }
        else
        {
            verticalVelocity.y += gravity * Time.deltaTime;
        }

        player.characterController.Move(verticalVelocity * Time.deltaTime);
    }

    private void HandleRotation()
    {
        if (!player.canRotate)
            return;
        
        targetRotationDirection = Vector3.zero;
        targetRotationDirection = PlayerCamera.instance.cameraObject.transform.forward * verticalMovement;
        targetRotationDirection = targetRotationDirection + PlayerCamera.instance.cameraObject.transform.right * horizontalMovement;
        targetRotationDirection.Normalize();
        targetRotationDirection.y = 0;

        if (targetRotationDirection == Vector3.zero)
        {
            targetRotationDirection = transform.forward;
        }
        
        Quaternion newRotation = Quaternion.LookRotation(targetRotationDirection);
        Quaternion targetRotation = Quaternion.Slerp(transform.rotation, newRotation, Time.deltaTime * rotationSpeed);
        transform.rotation = targetRotation;
    }

    private float ApplyJuiceToSpeed(float speed)
    {
        if (PlayerJuiceManager.instance == null)
            return speed;

        float juice = Mathf.Clamp(PlayerJuiceManager.instance.currentJuice, 0f, 100f);
        speed += Mathf.Lerp(0f, maxJuiceMovementBonus, juice / 100f);
        
        return speed;
    }
}
