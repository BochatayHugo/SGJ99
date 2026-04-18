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
    [Tooltip("Axis of rotation in LeverStick LOCAL space used to tilt the lever. Default (1,0,0) = local X.")]
    [SerializeField] private Vector3 leverRotationAxis = Vector3.right;
    [Tooltip("Angle offset in degrees applied at MINIMUM height. 0 = lever stays exactly at its scene position.")]
    [SerializeField] private float minLeverAngle = 0f;
    [Tooltip("Angle offset in degrees applied at MAXIMUM height.")]
    [SerializeField] private float maxLeverAngle = 40f;
    [Tooltip("TextMeshProUGUI inside HeightScreen/HeightCanvas/HeightText.")]
    [SerializeField] private TextMeshProUGUI heightDisplayText;

    // Rotation de repos du LeverStick, capturée à l'Awake avant toute modification.
    private Quaternion leverRestRotation;

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
    [Tooltip("Canvas affiché spécifiquement quand on survole le levier hauteur ou le bouton angle.")]
    [SerializeField] private GameObject mouseInteractCanvas;

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

    private void Awake()
    {
        // Capture la rotation d'origine du levier avant que quoi que ce soit ne la modifie.
        if (heightLeverStick != null)
            leverRestRotation = heightLeverStick.localRotation;
    }

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
        SetMouseCanvasVisible(false);
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
        ApplyLeverRotation(HeightMin);
        if (angleKnob != null)
            angleKnob.localEulerAngles = new Vector3(0f, -AngleMin, 0f);
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

            // MouseCanvasInteract pour levier et knob angle, CanvasInteract pour le reste
            bool isScrollTarget = currentHover == HoverTarget.HeightLever || currentHover == HoverTarget.AngleKnob;
            bool isAnyTarget    = currentHover != HoverTarget.None;

            SetMouseCanvasVisible(isScrollTarget);
            SetCanvasVisible(isAnyTarget && !isScrollTarget);
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
            // Knob rotates on local Y axis. Scroll up → counter-clockwise (negative Y).
            // Flip the sign here if the physical knob parent is oriented differently.
            angleKnob.localEulerAngles = new Vector3(0f, -currentWallAngle, 0f);
        }
    }

    private void RefreshAngleDisplay()
    {
        if (angleDisplayText != null)
            angleDisplayText.text = $"{currentWallAngle}°";
    }

    // ── Height lever ──────────────────────────────────────────────────────────

    /// <summary>
    /// Applique la rotation du levier en additionnant un offset angulaire
    /// sur leverRotationAxis (espace local du LeverStick), depuis sa rotation de repos.
    /// Utilise des quaternions — aucune conversion Euler, aucune dérive possible.
    /// </summary>
    private void ApplyLeverRotation(int height)
    {
        if (heightLeverStick == null) return;

        float t = Mathf.InverseLerp(HeightMin, HeightMax, height);
        float angleDeg = Mathf.Lerp(minLeverAngle, maxLeverAngle, t);

        // leverRestRotation * delta local = rotation finale dans l'espace parent.
        heightLeverStick.localRotation = leverRestRotation * Quaternion.AngleAxis(angleDeg, leverRotationAxis.normalized);
    }

    private void AdjustHeight(int direction)
    {
        currentWallHeight = Mathf.Clamp(currentWallHeight + direction * HeightStep, HeightMin, HeightMax);
        RefreshHeightDisplay();
        ApplyLeverRotation(currentWallHeight);
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

    private void SetMouseCanvasVisible(bool visible)
    {
        if (mouseInteractCanvas != null)
            mouseInteractCanvas.SetActive(visible);
    }
}
