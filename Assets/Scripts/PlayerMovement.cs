using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    private PlayerInput _inputs;
    private Transform _mainCamera;
    [SerializeField] private CharacterController _characterController;

    [SerializeField] private float _currentPlayerSpeed = 5f;
    [SerializeField] private float _walkPlayerSpeed = 5f;
    [SerializeField] private float _sprintPlayerSpeed = 8f;
    [SerializeField] private float _crouchPlayerSpeed = 3f;
    [SerializeField] private float defaultGravity = 9.81f;
    private float _verticalVelocity;

    private void Awake()
    {
        _inputs = new PlayerInput();    
    }

    private void Start()
    {
        _mainCamera = Camera.main.transform;
    }

    private void OnEnable()
    {
        _inputs.Enable();
    }

    private void OnDisable()
    {
        _inputs.Disable();
    }

    private void Update()
    {
        Vector2 inputVector = _inputs.Player.Move.ReadValue<Vector2>();
        Vector3 cameraForward = _mainCamera.forward;
        Vector3 cameraRight = _mainCamera.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = (cameraForward * inputVector.y) + (cameraRight * inputVector.x);

        if (_characterController.isGrounded)
        {
            _verticalVelocity = -2f;
            if (_inputs.Player.Jump.WasPressedThisFrame())
            {
                _verticalVelocity = 5.5f;
            }
        }
        else
        {
            _verticalVelocity -= defaultGravity * Time.deltaTime; 
        }

        bool isCrouchPressed = _inputs.Player.Crouch.IsPressed(); 
        bool isSprintPressed = _inputs.Player.Sprint.IsPressed();

        if (isCrouchPressed)
        {
            _currentPlayerSpeed = _crouchPlayerSpeed;
        }
        else if (isSprintPressed && inputVector.y > 0f) //Temporarily only run forward
        {
            _currentPlayerSpeed = _sprintPlayerSpeed;
        }
        else
        {
            _currentPlayerSpeed = _walkPlayerSpeed;
        }

        Vector3 finalMovementVector = moveDirection * _currentPlayerSpeed;
        finalMovementVector.y = _verticalVelocity;

        _characterController.Move(finalMovementVector * Time.deltaTime);

    }


}
