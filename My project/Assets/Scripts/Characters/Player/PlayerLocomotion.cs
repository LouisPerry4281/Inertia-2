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
    [SerializeField] float baseMovementSpeed = 5;
    [SerializeField] float rotationSpeed = 15;
    
    private void Awake()
    {
        player = GetComponent<PlayerManager>();
    }

    public void HandleAllMovement()
    {
        if (player.isPerformingAction)
            return;

        HandleGroundMovement();
        HandleRotation();
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
        
        player.characterController.Move(moveDirection * finalMovementSpeed * Time.deltaTime);
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
        float juice = PlayerJuiceManager.instance.currentJuice;

        speed *= (juice / 50) + baseMovementSpeed;
        
        return speed;
    }
}
