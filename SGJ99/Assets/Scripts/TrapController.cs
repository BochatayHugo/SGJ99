using UnityEngine;

/// <summary>
/// Controls the trap door opening and closing via a pivot rotation (bottom-up swing).
/// Attach this to the Trap GameObject.
/// </summary>
public class TrapController : MonoBehaviour
{
    [Header("Pivot Settings")]
    [Tooltip("The pivot point around which the trap door rotates (bottom edge).")]
    [SerializeField] private Transform pivotPoint;

    [Header("Rotation Settings")]
    [Tooltip("Target X rotation angle when the trap is fully open (e.g. -90 for a full horizontal door opening upward).")]
    [SerializeField] private float openAngle = -90f;

    [Tooltip("Duration in seconds for the open/close animation.")]
    [SerializeField] private float animationDuration = 0.8f;

    [Tooltip("Easing curve for the rotation animation.")]
    [SerializeField] private AnimationCurve easingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    private bool isOpen = false;
    private bool isAnimating = false;

    private float closedAngle = 0f;
    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private float animationTimer = 0f;
    private float startAngle = 0f;

    private void Start()
    {
        closedAngle = pivotPoint != null ? pivotPoint.localEulerAngles.x : transform.localEulerAngles.x;
        currentAngle = closedAngle;
    }

    private void Update()
    {
        if (!isAnimating) return;

        animationTimer += Time.deltaTime;
        float t = Mathf.Clamp01(animationTimer / animationDuration);
        float easedT = easingCurve.Evaluate(t);

        currentAngle = Mathf.LerpAngle(startAngle, targetAngle, easedT);

        if (pivotPoint != null)
        {
            pivotPoint.localEulerAngles = new Vector3(currentAngle, pivotPoint.localEulerAngles.y, pivotPoint.localEulerAngles.z);
        }
        else
        {
            transform.localEulerAngles = new Vector3(currentAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);
        }

        if (t >= 1f)
        {
            isAnimating = false;
        }
    }

    /// <summary>
    /// Toggles the trap between open and closed states.
    /// </summary>
    public void Toggle()
    {
        if (isAnimating) return;

        isOpen = !isOpen;
        startAngle = currentAngle;
        targetAngle = isOpen ? openAngle : closedAngle;
        animationTimer = 0f;
        isAnimating = true;
    }

    /// <summary>
    /// Returns whether the trap is currently open.
    /// </summary>
    public bool IsOpen => isOpen;
}
