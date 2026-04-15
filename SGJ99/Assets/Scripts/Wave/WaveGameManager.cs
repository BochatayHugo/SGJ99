using UnityEngine;
using System;

/// <summary>
/// Generates a random wave event at game start and evaluates the player's responses.
/// Broadcasts events so all UI panels can react independently.
/// </summary>
public class WaveGameManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WaveDataSO waveData;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired once after wave data is generated.</summary>
    public static event Action<WaveDataSO> OnWaveDataGenerated;

    /// <summary>Fired when the game ends. True = evacuation needed, False = false alarm.</summary>
    public static event Action<GameOverReason> OnGameOver;

    // ── State ────────────────────────────────────────────────────────────────

    private bool playerSubmitted = false;

    private void Start()
    {
        GenerateWave();
    }

    /// <summary>Generates random wave data and broadcasts it to all listeners.</summary>
    public void GenerateWave()
    {
        waveData.waveHeight = RoundToStep(UnityEngine.Random.Range(5, 51), 5);
        waveData.waveAngle  = RoundToStep(UnityEngine.Random.Range(0, 181), 10);
        waveData.pressureValue = UnityEngine.Random.Range(1, 321);
        waveData.epicenterZoneIndex = UnityEngine.Random.Range(0, 16);

        playerSubmitted = false;
        OnWaveDataGenerated?.Invoke(waveData);
    }

    /// <summary>
    /// Called by DeskController when the player submits their settings.
    /// wallAngle and wallHeight are the values the player dialed in.
    /// pressureCombination is the bitmask of the 4 buttons the player activated.
    /// evacuationPressed indicates whether the player hit the evacuation button.
    /// </summary>
    public void SubmitPlayerResponse(
        int wallAngle,
        int wallHeight,
        int pressureCombination,
        bool evacuationPressed)
    {
        if (playerSubmitted) return;
        playerSubmitted = true;

        bool evacuationRequired = waveData.RequiresEvacuation;

        if (evacuationPressed)
        {
            if (!evacuationRequired)
            {
                OnGameOver?.Invoke(GameOverReason.FalseAlarm);
            }
            else
            {
                // Evacuation was valid — could trigger a success state
                OnGameOver?.Invoke(GameOverReason.EvacuationSuccess);
            }
            return;
        }

        // Player did not evacuate — check each value
        bool angleCorrect    = wallAngle == waveData.waveAngle;
        bool heightCorrect   = wallHeight == waveData.waveHeight;
        bool pressureCorrect = pressureCombination == waveData.RequiredPressureCombination;

        if (!angleCorrect || !heightCorrect || !pressureCorrect || evacuationRequired)
        {
            OnGameOver?.Invoke(GameOverReason.CityDestroyed);
        }
        else
        {
            OnGameOver?.Invoke(GameOverReason.Success);
        }
    }

    private int RoundToStep(int value, int step)
    {
        return Mathf.RoundToInt(value / (float)step) * step;
    }
}

public enum GameOverReason
{
    Success,
    CityDestroyed,  // Player failed to act correctly
    FalseAlarm,     // Player evacuated when not needed → fired
    EvacuationSuccess
}
