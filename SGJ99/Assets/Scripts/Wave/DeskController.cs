using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

/// <summary>
/// Manages all interactive controls on the ControlerDesk:
///   - Rotary knob  → wall angle  (0–180°, steps of 10°)
///   - Height lever → wall height (5–50m,  steps of 5m)
///   - 4 pressure buttons (bitmask combination)
///   - Evacuation button
/// Attach to the ControlerDesk GameObject.
/// </summary>
public class DeskController : MonoBehaviour
{
    // ── References ────────────────────────────────────────────────────────────

    [Header("Wave Data")]
    [SerializeField] private WaveDataSO waveData;
    [SerializeField] private WaveGameManager gameManager;

    [Header("Angle Knob")]
    [Tooltip("The rotating knob Transform — assign KnobBase.")]
    [SerializeField] private Transform angleKnob;
    [Tooltip("TextMeshProUGUI inside AngleScreen/AngleCanvas/AngleText.")]
    [SerializeField] private TextMeshProUGUI angleDisplayText;

    [Header("Height Lever")]
    [Tooltip("The lever STICK Transform only — LeverBase stays fixed, only LeverStick rotates.")]
    [SerializeField] private Transform heightLeverStick;
    [Tooltip("Lever rotation in degrees at minimum height (5m). Positive = forward tilt.")]
    [SerializeField] private float minLeverAngle = 45f;
    [Tooltip("Lever rotation in degrees at maximum height (50m). Negative = back tilt.")]
    [SerializeField] private float maxLeverAngle = -45f;
    [Tooltip("TextMeshProUGUI inside HeightScreen/HeightCanvas/HeightText.")]
    [SerializeField] private TextMeshProUGUI heightDisplayText;

    [Header("Pressure Buttons")]
    [Tooltip("4 button renderers (index 0–3 = button 1–4).")]
    [SerializeField] private Renderer[] pressureButtonRenderers = new Renderer[4];
    [SerializeField] private Material buttonOnMaterial;
    [SerializeField] private Material buttonOffMaterial;

    [Header("Evacuation Button")]
    [SerializeField] private Renderer evacuationButtonRenderer;
    [SerializeField] private Material evacuationReadyMaterial;
    [SerializeField] private Material evacuationPressedMaterial;

    [Header("OK Button")]
    [Tooltip("The OKBtn renderer — player presses E to confirm settings.")]
    [SerializeField] private Renderer okButtonRenderer;
    [SerializeField] private Material okReadyMaterial;
    [SerializeField] private Material okPressedMaterial;

    [Header("Interaction")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private float aimTolerance = 0.12f;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject interactCanvas;

    // ── State ────────────────────────────────────────────────────────────────

    private const int AngleMin  = 0;
    private const int AngleMax  = 180;
    private const int AngleStep = 10;
    private const int HeightMin = 5;
    private const int HeightMax = 50;
    private const int HeightStep = 5;

    private int currentWallAngle  = 90;
    private int currentWallHeight = 25;
    private int pressureBitmask   = 0;   // bits 0–3 for buttons 1–4
    private bool evacuated        = false;

    private InputAction interactAction;
    private InputAction scrollAction;

    // Hovered control type
    private HoverTarget currentHover = HoverTarget.None;

    private enum HoverTarget { None, AngleKnob, HeightLever, PressureBtn0, PressureBtn1, PressureBtn2, PressureBtn3, EvacuationBtn, OKBtn }

    private void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;

        PlayerInput playerInput = FindFirstObjectByType<PlayerInput>();
        if (playerInput != null)
        {
            interactAction = playerInput.actions["Interact"];
            // Use scroll wheel or left/right arrows to adjust knob and lever
            scrollAction = playerInput.actions.FindAction("Scroll", false);
        }

        RefreshAngleDisplay();
        RefreshHeightDisplay();
        RefreshPressureButtons();
        RefreshEvacuationButton();
        SetCanvasVisible(false);
    }

    private void OnEnable()
    {
        WaveGameManager.OnWaveDataGenerated += OnWaveDataReceived;
    }

    private void OnDisable()
    {
        WaveGameManager.OnWaveDataGenerated -= OnWaveDataReceived;
    }

    private void OnWaveDataReceived(WaveDataSO data)
    {
        evacuated         = false;
        pressureBitmask   = 0;
        currentWallAngle  = AngleMin;
        currentWallHeight = HeightMin;
        RefreshAngleDisplay();
        RefreshHeightDisplay();
        RefreshPressureButtons();
        RefreshEvacuationButton();
        RefreshOKButton();

        // Reset lever and knob visual to starting position
        if (heightLeverStick != null)
            heightLeverStick.localEulerAngles = new Vector3(minLeverAngle, 0f, 0f);
        if (angleKnob != null)
            angleKnob.localEulerAngles = new Vector3(0f, 0f, AngleMin);
    }

    private void Update()
    {
        CheckHover();
        HandleScrollInput();
        HandleInteractInput();
    }

    // ── Hover detection ──────────────────────────────────────────────────────

    private void CheckHover()
    {
        if (playerCamera == null) return;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        HoverTarget newHover = HoverTarget.None;

        if (Physics.SphereCast(ray, aimTolerance, out RaycastHit hit, interactDistance))
        {
            newHover = IdentifyHoverTarget(hit.collider.gameObject);
        }

        if (newHover != currentHover)
        {
            currentHover = newHover;
            SetCanvasVisible(currentHover != HoverTarget.None);
        }
    }

    private HoverTarget IdentifyHoverTarget(GameObject go)
    {
        if (angleKnob   != null && go == angleKnob.gameObject)        return HoverTarget.AngleKnob;
        if (heightLeverStick != null && go == heightLeverStick.gameObject) return HoverTarget.HeightLever;

        for (int i = 0; i < pressureButtonRenderers.Length; i++)
            if (pressureButtonRenderers[i] != null && go == pressureButtonRenderers[i].gameObject)
                return HoverTarget.PressureBtn0 + i;

        if (evacuationButtonRenderer != null && go == evacuationButtonRenderer.gameObject)
            return HoverTarget.EvacuationBtn;

        if (okButtonRenderer != null && go == okButtonRenderer.gameObject)
            return HoverTarget.OKBtn;

        return HoverTarget.None;
    }

    // ── Scroll input (knob / lever) ──────────────────────────────────────────

    private void HandleScrollInput()
    {
        float scroll = 0f;

        if (scrollAction != null)
        {
            scroll = scrollAction.ReadValue<Vector2>().y;
        }
        else
        {
            // Fallback: mouse scroll via legacy input
            scroll = Input.GetAxis("Mouse ScrollWheel");
        }

        if (Mathf.Approximately(scroll, 0f)) return;

        int direction = scroll > 0 ? 1 : -1;

        switch (currentHover)
        {
            case HoverTarget.AngleKnob:
                AdjustAngle(direction);
                break;
            case HoverTarget.HeightLever:
                AdjustHeight(direction);
                break;
        }
    }

    // ── Interact input (pressure buttons / evacuation) ───────────────────────

    private void HandleInteractInput()
    {
        if (interactAction == null || !interactAction.WasPressedThisFrame()) return;

        switch (currentHover)
        {
            case HoverTarget.PressureBtn0:
            case HoverTarget.PressureBtn1:
            case HoverTarget.PressureBtn2:
            case HoverTarget.PressureBtn3:
                int btnIndex = currentHover - HoverTarget.PressureBtn0;
                TogglePressureButton(btnIndex);
                break;

            case HoverTarget.EvacuationBtn:
                TriggerEvacuation();
                break;

            case HoverTarget.OKBtn:
                TriggerOK();
                break;
        }
    }

    // ── Angle knob ────────────────────────────────────────────────────────────

    private void AdjustAngle(int direction)
    {
        currentWallAngle = Mathf.Clamp(currentWallAngle + direction * AngleStep, AngleMin, AngleMax);
        RefreshAngleDisplay();

        if (angleKnob != null)
        {
            // Positive scroll → knob turns clockwise (positive Z rotation)
            angleKnob.localEulerAngles = new Vector3(0f, 0f, currentWallAngle);
        }
    }

    private void RefreshAngleDisplay()
    {
        if (angleDisplayText != null)
            angleDisplayText.text = $"{currentWallAngle}°";
    }

    // ── Height lever ──────────────────────────────────────────────────────────

    private void AdjustHeight(int direction)
    {
        currentWallHeight = Mathf.Clamp(currentWallHeight + direction * HeightStep, HeightMin, HeightMax);
        RefreshHeightDisplay();

        if (heightLeverStick != null)
        {
            // Only the stick rotates; LeverBase stays fixed.
            // Scroll up → lever tilts forward (positive X), scroll down → backward.
            float t = Mathf.InverseLerp(HeightMin, HeightMax, currentWallHeight);
            float leverAngle = Mathf.Lerp(minLeverAngle, maxLeverAngle, t);
            heightLeverStick.localEulerAngles = new Vector3(leverAngle, 0f, 0f);
        }
    }

    private void RefreshHeightDisplay()
    {
        if (heightDisplayText != null)
            heightDisplayText.text = $"{currentWallHeight} m";
    }

    // ── Pressure buttons ──────────────────────────────────────────────────────

    private void TogglePressureButton(int index)
    {
        pressureBitmask ^= (1 << index);
        RefreshPressureButtons();
    }

    private void RefreshPressureButtons()
    {
        for (int i = 0; i < pressureButtonRenderers.Length; i++)
        {
            if (pressureButtonRenderers[i] == null) continue;
            bool on = (pressureBitmask & (1 << i)) != 0;
            pressureButtonRenderers[i].material = on ? buttonOnMaterial : buttonOffMaterial;
        }
    }

    // ── Evacuation button ─────────────────────────────────────────────────────

    private void TriggerEvacuation()
    {
        if (evacuated) return;
        evacuated = true;

        if (evacuationButtonRenderer != null)
            evacuationButtonRenderer.material = evacuationPressedMaterial;

        gameManager.SubmitPlayerResponse(currentWallAngle, currentWallHeight, pressureBitmask, true);
    }

    private void RefreshEvacuationButton()
    {
        evacuated = false;
        if (evacuationButtonRenderer != null)
            evacuationButtonRenderer.material = evacuationReadyMaterial;
    }

    // ── OK button ─────────────────────────────────────────────────────────────

    private void TriggerOK()
    {
        if (okButtonRenderer != null)
            okButtonRenderer.material = okPressedMaterial;

        gameManager.SubmitPlayerResponse(currentWallAngle, currentWallHeight, pressureBitmask, false);
    }

    private void RefreshOKButton()
    {
        if (okButtonRenderer != null)
            okButtonRenderer.material = okReadyMaterial;
    }

    /// <summary>Submits the current settings without evacuation.</summary>
    public void SubmitSettings()
    {
        gameManager.SubmitPlayerResponse(currentWallAngle, currentWallHeight, pressureBitmask, false);
    }

    // ── Canvas ────────────────────────────────────────────────────────────────

    private void SetCanvasVisible(bool visible)
    {
        if (interactCanvas != null)
            interactCanvas.SetActive(visible);
    }
}
