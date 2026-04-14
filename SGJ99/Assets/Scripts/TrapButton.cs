using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player look-at detection for the trap button using a raycast from the player camera.
/// Shows the interact canvas when the player is looking at the button,
/// and triggers the trap toggle when the Interact action is pressed.
/// Attach this to the TrapBTN GameObject.
/// </summary>
public class TrapButton : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The trap controlled by this button.")]
    [SerializeField] private TrapController trapController;

    [Tooltip("The canvas to display when the player is looking at this button.")]
    [SerializeField] private GameObject interactCanvas;

    [Header("Detection Settings")]
    [Tooltip("Maximum distance from which the player can interact with the button.")]
    [SerializeField] private float interactDistance = 3f;

    [Tooltip("Radius of the sphere used for aim tolerance detection. Increase if the button is hard to target.")]
    [SerializeField] private float aimTolerance = 0.15f;

    [Tooltip("Camera used for the raycast detection (usually the Main Camera).")]
    [SerializeField] private Camera playerCamera;

    private bool isLookedAt = false;
    private InputAction interactAction;

    private void Start()
    {
        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
        }
        else
        {
            Debug.LogError("[TrapButton] No PlayerInput found in the scene.");
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (interactCanvas != null)
        {
            interactCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        CheckLookAt();

        if (isLookedAt && interactAction != null && interactAction.WasPressedThisFrame())
        {
            trapController.Toggle();
        }
    }

    private void CheckLookAt()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        // SphereCast adds aim tolerance around the crosshair, making small objects much easier to target.
        bool hitThisButton = Physics.SphereCast(ray, aimTolerance, out RaycastHit hit, interactDistance)
                             && hit.collider.gameObject == gameObject;

        if (hitThisButton && !isLookedAt)
        {
            isLookedAt = true;
            SetCanvasVisible(true);
        }
        else if (!hitThisButton && isLookedAt)
        {
            isLookedAt = false;
            SetCanvasVisible(false);
        }
    }

    private void SetCanvasVisible(bool visible)
    {
        if (interactCanvas != null)
        {
            interactCanvas.SetActive(visible);
        }
    }
}
