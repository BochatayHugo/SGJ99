using UnityEngine;
using System;

/// <summary>
/// Orchestrates the endless day-by-day gameplay loop.
/// Each day generates a new wave. The player must either:
///   - Submit calibrated settings via OKBtn (if no evacuation needed), or
///   - Press the evacuation button (if values exceed controllable range).
/// A successful day unlocks the PRO_Lit to start the next day.
/// </summary>
public class WaveGameManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private WaveDataSO waveData;

    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fired once after wave data is generated for a new day.</summary>
    public static event Action<WaveDataSO> OnWaveDataGenerated;

    /// <summary>Fired when the player submits and the day succeeds. Payload = current day number.</summary>
    public static event Action<int> OnDaySuccess;

    /// <summary>Fired when the player loses. Carries the reason.</summary>
    public static event Action<GameOverReason> OnGameOver;

    // ── State ─────────────────────────────────────────────────────────────────

    private int  currentDay       = 0;
    private bool playerSubmitted  = false;
    private bool daySucceeded     = false;

    /// <summary>Whether the player cleared the current day and may interact with PRO_Lit.</summary>
    public bool CanAdvanceToNextDay => daySucceeded;

    /// <summary>Current day number (1-based).</summary>
    public int CurrentDay => currentDay;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Start()
    {
        StartDay(1);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by PRO_Lit interaction to advance to the next day.
    /// Only works when the current day has been successfully cleared.
    /// </summary>
    public void AdvanceToNextDay()
    {
        if (!daySucceeded) return;
        StartDay(currentDay + 1);
    }

    /// <summary>
    /// Called by DeskController when the player submits their settings.
    /// evacuationPressed = true  → player used the evacuation button.
    /// evacuationPressed = false → player used the OKBtn to confirm calibration.
    /// </summary>
    public void SubmitPlayerResponse(
        int  wallAngle,
        int  wallHeight,
        int  pressureCombination,
        bool evacuationPressed)
    {
        if (playerSubmitted) return;
        playerSubmitted = true;

        bool evacuationRequired = waveData.RequiresEvacuation;

        if (evacuationPressed)
        {
            if (evacuationRequired)
            {
                SucceedDay();
            }
            else
            {
                OnGameOver?.Invoke(GameOverReason.FalseAlarm);
            }
            return;
        }

        // Player pressed OK without evacuating
        if (evacuationRequired)
        {
            OnGameOver?.Invoke(GameOverReason.CityDestroyed);
            return;
        }

        bool angleCorrect    = wallAngle           == waveData.waveAngle;
        bool heightCorrect   = wallHeight          == waveData.waveHeight;
        bool pressureCorrect = pressureCombination == waveData.RequiredPressureCombination;

        if (angleCorrect && heightCorrect && pressureCorrect)
        {
            SucceedDay();
        }
        else
        {
            OnGameOver?.Invoke(GameOverReason.CityDestroyed);
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void StartDay(int day)
    {
        currentDay      = day;
        playerSubmitted = false;
        daySucceeded    = false;
        GenerateWave();
    }

    private void GenerateWave()
    {
        waveData.waveHeight         = RoundToStep(UnityEngine.Random.Range(5, 51), 5);
        waveData.waveAngle          = RoundToStep(UnityEngine.Random.Range(0, 181), 10);
        waveData.pressureValue      = UnityEngine.Random.Range(1, 321);
        waveData.epicenterZoneIndex = UnityEngine.Random.Range(0, 16);
        OnWaveDataGenerated?.Invoke(waveData);
    }

    private void SucceedDay()
    {
        daySucceeded = true;
        OnDaySuccess?.Invoke(currentDay);
    }

    private int RoundToStep(int value, int step)
    {
        return Mathf.RoundToInt(value / (float)step) * step;
    }
}

public enum GameOverReason
{
    CityDestroyed,
    FalseAlarm,
    EvacuationSuccess
}
