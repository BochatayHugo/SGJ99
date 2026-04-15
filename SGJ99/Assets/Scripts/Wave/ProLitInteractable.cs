using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attached to the PRO_Lit GameObject.
/// When the player looks at it and presses E after a successful day,
/// it triggers the next day via WaveGameManager.
/// Shows the interactCanvas hint only when the day has been successfully completed.
/// </summary>
public class ProLitInteractable : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WaveGameManager gameManager;
    [Tooltip("The CanvasInteract hint shown when the player looks at PRO_Lit.")]
    [SerializeField] private GameObject interactCanvas;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 4f;
    [SerializeField] private float aimTolerance     = 0.2f;
    [SerializeField] private Camera playerCamera;

    private bool        isLookedAt    = false;
    private InputAction interactAction;

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
            interactAction = playerInput.actions["Interact"];

        if (interactCanvas != null)
            interactCanvas.SetActive(false);
    }

    private void Update()
    {
        CheckLookAt();

        if (isLookedAt && gameManager != null && gameManager.CanAdvanceToNextDay
            && interactAction != null && interactAction.WasPressedThisFrame())
        {
            gameManager.AdvanceToNextDay();
        }
    }

    private void CheckLookAt()
    {
        if (playerCamera == null) return;

        // Only show the interact hint when the day has been cleared
        bool dayCleared = gameManager != null && gameManager.CanAdvanceToNextDay;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hitThis = Physics.SphereCast(ray, aimTolerance, out RaycastHit hit, interactDistance)
                       && hit.collider.gameObject == gameObject;

        bool shouldShow = hitThis && dayCleared;

        if (shouldShow != isLookedAt)
        {
            isLookedAt = shouldShow;
            if (interactCanvas != null)
                interactCanvas.SetActive(isLookedAt);
        }
    }
}
