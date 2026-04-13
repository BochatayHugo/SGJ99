using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    public static PlayerController instance;
    private PlayerStateMachine stateMachine;

    // Player parameters
    [SerializeField] private CharacterController player;
    [SerializeField] private float moveSpeed;

    private PlayerInput playerInput;        
    private InputAction moveAction;           
    private InputAction crouchAction;

    void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        stateMachine = new PlayerStateMachine(player, transform, moveSpeed);

        playerInput = GetComponent<PlayerInput>();
        moveAction = playerInput.actions["Move"];
        crouchAction = playerInput.actions["Crouch"];

    }

    void Update()
    {
        Vector2 input = moveAction.ReadValue<Vector2>();
        bool crouchPressed = crouchAction.IsPressed();
        stateMachine.setInfos(input,crouchPressed);
        stateMachine.Update();
    }
}
