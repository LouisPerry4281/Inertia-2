using UnityEngine;

public class CharacterManager : MonoBehaviour
{
    [HideInInspector] public CharacterController characterController;
    
    [Header("Flags")] 
    public bool isPerformingAction = false;
    public bool canRotate = true;
    public bool canMove = true;
    
    protected virtual void Awake()
    {
        DontDestroyOnLoad(gameObject);
        
        characterController = GetComponent<CharacterController>();
    }
}
