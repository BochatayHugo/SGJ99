using UnityEngine;

public class PlayerStateMachine
{
    public enum PlayerState
    {
        Idle,
        Walking,
        Running,
        Falling,
        Crouching,
        Working,
        Interacting,
        Sleeping,
        Listening
    }

    private CharacterController characterController;
    private Transform playerTransform;
    private PlayerState currentState;
    
    private float moveSpeed;
    private float verticalVelocity;
    private Vector3 moveDirection;
    private bool crouch;


    private float gravity = -9.81f;
    public PlayerStateMachine(CharacterController characterController, Transform transform, float speed)
    {
        this.characterController = characterController;
        this.playerTransform = transform;
        this.moveSpeed = speed;

        ChangeState(PlayerState.Idle);
    }

    public void setInfos(Vector2 inputDirection,bool crouchPressed)
    {
        moveDirection = inputDirection;
        crouch = crouchPressed;
    }

    public void Update()
    {
        Debug.Log("Current State: " + currentState);
        ApplyGravity();
        switch (currentState)
        {
            case PlayerState.Idle:
                DoIdle();
                break;
            case PlayerState.Walking:
                DoWalking();
                break;
            case PlayerState.Running:
                DoRunning();
                break;
            case PlayerState.Falling:
                DoFalling();
                break;
            case PlayerState.Crouching:
                DoCrouching();
                break;
        }
    }

    public void ChangeState(PlayerState newState)
    {
        OnExit(currentState);
        currentState = newState;
        OnEnter(currentState);
    }
    private void OnEnter(PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Idle:
                
                break;
            case PlayerState.Walking:
                //setup
                break;
            case PlayerState.Running:
                //setup
                break;
            case PlayerState.Falling:
                //setup
                break;

        }
    }
    private void OnExit(PlayerState newState)
    {
        switch (newState)
        {
            case PlayerState.Idle:
                //setup
                break;
            case PlayerState.Walking:
                //setup
                break;
            case PlayerState.Running:
                //setup
                break;
            case PlayerState.Falling:
                //setup
                break;

        }
    }

    private void DoIdle()
    {
        if(moveDirection.magnitude > 0.1f)
        {
            ChangeState(PlayerState.Walking);
        }
        else if(crouch)
        {
            ChangeState(PlayerState.Crouching);
        }
        else if(!characterController.isGrounded)
        {
            ChangeState(PlayerState.Falling);
        }
        else
        {
            Vector3 move = Vector3.up * verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }
    }
    private void DoWalking()
    {
        if (moveDirection.magnitude < 0.1f)
        {
            ChangeState(PlayerState.Idle);
        }
        else if(crouch)
        {
            ChangeState(PlayerState.Crouching);
        }
        else if (!characterController.isGrounded)
        {
            ChangeState(PlayerState.Falling);
        }
        else
        {
            Vector3 move = new Vector3(moveDirection.x, 0, moveDirection.y) ;
            Vector3 direction = playerTransform.TransformDirection(move);
            Vector3 motion = direction.normalized * moveSpeed + Vector3.up * verticalVelocity;
            characterController.Move(motion * Time.deltaTime);
        }
    }   
    private void DoRunning()
    {

    }
    private void DoFalling()
    {
        if (characterController.isGrounded)
        {
            if(characterController.velocity.magnitude > 0.1f)
            {
                ChangeState(PlayerState.Walking);
            }
            else
                ChangeState(PlayerState.Idle);
        }
        else
        {
            Vector3 move = new Vector3(0, verticalVelocity, 0);
            characterController.Move(move * Time.deltaTime);
        }
    }
     private void DoCrouching()
    {

    }
     private void DoWorking()
    {
    }
     private void DoInteracting()
    {
    }
     private void DoSleeping()
    {
    }
     private void DoListening()
    {
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0)
        {
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }
    }
}

