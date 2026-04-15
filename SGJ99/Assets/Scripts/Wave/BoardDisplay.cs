using UnityEngine;
using TMPro;

/// <summary>
/// Renders the information board:
/// - A dynamic wave drawing (angle + height) using a LineRenderer on a child WaveDrawing object
/// - The epicenter zone name, pressure, height and angle via TextMeshProUGUI inside a World Space Canvas
/// Attach to the Board GameObject.
/// </summary>
public class BoardDisplay : MonoBehaviour
{
    [Header("Wave Drawing")]
    [Tooltip("LineRenderer child object used to draw the wave (assign WaveDrawing child).")]
    [SerializeField] private LineRenderer waveLineRenderer;

    [Tooltip("Number of points used to draw the sine wave.")]
    [SerializeField] private int waveResolution = 64;

    [Tooltip("Width of the wave drawing area in local units.")]
    [SerializeField] private float drawWidth = 0.8f;

    [Tooltip("Max amplitude in local units — maps from min to max wave height.")]
    [SerializeField] private float maxAmplitude = 0.15f;

    [Header("UI Labels (TextMeshProUGUI inside BoardCanvas)")]
    [Tooltip("Text showing the epicenter zone name.")]
    [SerializeField] private TextMeshProUGUI zoneNameText;

    [Tooltip("Text showing the pressure value.")]
    [SerializeField] private TextMeshProUGUI pressureText;

    [Tooltip("Text showing the wave height.")]
    [SerializeField] private TextMeshProUGUI waveHeightText;

    [Tooltip("Text showing the wave angle.")]
    [SerializeField] private TextMeshProUGUI waveAngleText;

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
        DrawWave(data.waveHeight, data.waveAngle);
        UpdateLabels(data);
    }

    /// <summary>Draws a sine wave whose amplitude maps to wave height and is rotated by waveAngle.</summary>
    private void DrawWave(int heightMeters, int angleDegrees)
    {
        if (waveLineRenderer == null) return;

        float amplitude = Mathf.InverseLerp(5, 50, heightMeters) * maxAmplitude;
        float angleRad  = angleDegrees * Mathf.Deg2Rad;

        waveLineRenderer.positionCount = waveResolution;

        // Thicker line for better readability on the board
        waveLineRenderer.startWidth = 0.04f;
        waveLineRenderer.endWidth   = 0.04f;

        for (int i = 0; i < waveResolution; i++)
        {
            float t = i / (float)(waveResolution - 1);
            float x = (t - 0.5f) * drawWidth;
            float y = Mathf.Sin(t * Mathf.PI * 2f) * amplitude;

            // Rotate the point around the origin by the wave angle
            float rotatedX = x * Mathf.Cos(angleRad) - y * Mathf.Sin(angleRad);
            float rotatedY = x * Mathf.Sin(angleRad) + y * Mathf.Cos(angleRad);

            waveLineRenderer.SetPosition(i, new Vector3(rotatedX, rotatedY, 0f));
        }
    }

    private void UpdateLabels(WaveDataSO data)
    {
        if (zoneNameText != null)
            zoneNameText.text = data.EpicenterZoneName;

        if (pressureText != null)
            pressureText.text = $"{data.pressureValue} Pa";

        if (waveHeightText != null)
            waveHeightText.text = $"{data.waveHeight} m";

        if (waveAngleText != null)
            waveAngleText.text = $"{data.waveAngle}°";
    }
}
