using UnityEngine;

/// <summary>
/// ScriptableObject holding all data for a generated wave event.
/// Acts as the shared data bus between the Board, LocalisationDesk, and ControlerDesk.
/// </summary>
[CreateAssetMenu(fileName = "WaveData", menuName = "Wave/WaveData")]
public class WaveDataSO : ScriptableObject
{
    // ── Wave parameters ──────────────────────────────────────────────────────

    /// <summary>Wave height in meters (5 to 50, steps of 5).</summary>
    [Range(5, 50)] public int waveHeight = 20;

    /// <summary>Wave inclination angle in degrees (0 to 180, steps of 10).</summary>
    [Range(0, 180)] public int waveAngle = 90;

    /// <summary>Underwater pressure value (20 to 320).</summary>
    [Range(20, 320)] public int pressureValue = 100;

    /// <summary>Index of the epicenter zone (0–15).</summary>
    [Range(0, 15)] public int epicenterZoneIndex = 0;

    // ── Thresholds (max values the controls can handle) ───────────────────────

    /// <summary>Max wall height the lever can reach.</summary>
    public const int MaxWallHeight = 50;

    /// <summary>Max wall angle the knob can reach.</summary>
    public const int MaxWallAngle = 180;

    /// <summary>Max pressure range index the buttons can represent.</summary>
    public const int MaxPressureRangeIndex = 15;

    // ── Zone names ───────────────────────────────────────────────────────────

    public static readonly string[] ZoneNames = new string[16]
    {
        "Narval",    "Orque",     "Pieuvre",   "Méduse",
        "Calamar",   "Baleine",   "Requin",    "Dauphin",
        "Murène",    "Raie",      "Hippocampe","Nautile",
        "Anémone",   "Langouste", "Turbot",    "Thon"
    };

    /// <summary>Returns the name of the current epicenter zone.</summary>
    public string EpicenterZoneName => ZoneNames[epicenterZoneIndex];

    // ── Pressure helpers ─────────────────────────────────────────────────────

    /// <summary>
    /// Returns the pressure range index (0–15) for a given pressure value.
    /// Ranges go 0–19, 20–39, … 300–319, 320+.
    /// </summary>
    public static int GetPressureRangeIndex(int pressure)
    {
        return Mathf.Clamp((pressure - 1) / 20, 0, 15);
    }

    /// <summary>
    /// Returns the button combination (bitmask, bits 0–3) for a given pressure range index.
    /// The 16 combinations of 4 buttons map 1-to-1 to the 16 pressure ranges (Grey code order
    /// so adjacent ranges differ by exactly one button, making errors more obvious).
    /// </summary>
    public static readonly int[] PressureRangeToCombination = new int[16]
    {
        0b0000, // range  0 (  1– 20)  → no buttons
        0b0001, // range  1 ( 21– 40)  → btn 1
        0b0011, // range  2 ( 41– 60)  → btn 1+2
        0b0010, // range  3 ( 61– 80)  → btn 2
        0b0110, // range  4 ( 81–100)  → btn 2+3
        0b0111, // range  5 (101–120)  → btn 1+2+3
        0b0101, // range  6 (121–140)  → btn 1+3
        0b0100, // range  7 (141–160)  → btn 3
        0b1100, // range  8 (161–180)  → btn 3+4
        0b1101, // range  9 (181–200)  → btn 1+3+4
        0b1111, // range 10 (201–220)  → btn 1+2+3+4
        0b1110, // range 11 (221–240)  → btn 2+3+4
        0b1010, // range 12 (241–260)  → btn 2+4
        0b1011, // range 13 (261–280)  → btn 1+2+4
        0b1001, // range 14 (281–300)  → btn 1+4
        0b1000, // range 15 (301–320)  → btn 4
    };

    /// <summary>Returns the required button bitmask for the current pressure.</summary>
    public int RequiredPressureCombination =>
        PressureRangeToCombination[GetPressureRangeIndex(pressureValue)];

    // ── Overflow check ───────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if at least one value exceeds what the controls can handle,
    /// meaning evacuation is mandatory.
    /// </summary>
    public bool RequiresEvacuation =>
        waveHeight > MaxWallHeight ||
        waveAngle > MaxWallAngle ||
        GetPressureRangeIndex(pressureValue) > MaxPressureRangeIndex;
}
