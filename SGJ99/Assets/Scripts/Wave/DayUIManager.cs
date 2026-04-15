using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Manages the day announcement UI canvas (e.g. "DAY 1", "DAY 2"…).
/// Shows a fade-in/out splash at the start of each day.
/// Also handles the Game Over canvas.
/// Attach to a persistent UI manager GameObject.
/// </summary>
public class DayUIManager : MonoBehaviour
{
    [Header("Day Announcement")]
    [Tooltip("The CanvasGroup on the Day announcement canvas (controls fade).")]
    [SerializeField] private CanvasGroup dayCanvasGroup;
    [Tooltip("Text element displaying the day number (e.g. 'DAY 1').")]
    [SerializeField] private TextMeshProUGUI dayText;
    [Tooltip("How long the day canvas stays fully visible.")]
    [SerializeField] private float holdDuration = 2f;
    [Tooltip("Fade in and out duration.")]
    [SerializeField] private float fadeDuration = 0.6f;

    [Header("Game Over")]
    [Tooltip("The Game Over canvas GameObject.")]
    [SerializeField] private GameObject gameOverCanvas;
    [Tooltip("Text element on the Game Over canvas.")]
    [SerializeField] private TextMeshProUGUI gameOverText;

    [Header("Day Success Hint")]
    [Tooltip("Small hint shown when day is cleared telling player to go to PRO_Lit.")]
    [SerializeField] private GameObject successHintCanvas;

    private static readonly string MsgCityDestroyed = "TOUTE LA VILLE A ÉTÉ DÉCIMÉE.\nVous avez échoué.";
    private static readonly string MsgFalseAlarm    = "FAUSSE ALERTE.\nVous avez été renvoyé.";

    private void OnEnable()
    {
        WaveGameManager.OnWaveDataGenerated += OnNewDay;
        WaveGameManager.OnDaySuccess        += OnDaySuccess;
        WaveGameManager.OnGameOver          += OnGameOver;
    }

    private void OnDisable()
    {
        WaveGameManager.OnWaveDataGenerated -= OnNewDay;
        WaveGameManager.OnDaySuccess        -= OnDaySuccess;
        WaveGameManager.OnGameOver          -= OnGameOver;
    }

    private void Start()
    {
        if (dayCanvasGroup != null)
        {
            dayCanvasGroup.alpha          = 0f;
            dayCanvasGroup.blocksRaycasts = false;
        }

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);

        if (successHintCanvas != null)
            successHintCanvas.SetActive(false);
    }

    // ── Event handlers ────────────────────────────────────────────────────────

    private void OnNewDay(WaveDataSO data)
    {
        // WaveGameManager fires this; we retrieve the day number from it
        WaveGameManager mgr = FindFirstObjectByType<WaveGameManager>();
        if (mgr == null) return;

        if (successHintCanvas != null)
            successHintCanvas.SetActive(false);

        StartCoroutine(ShowDayAnnouncement(mgr.CurrentDay));
    }

    private void OnDaySuccess(int day)
    {
        if (successHintCanvas != null)
            successHintCanvas.SetActive(true);
    }

    private void OnGameOver(GameOverReason reason)
    {
        if (successHintCanvas != null)
            successHintCanvas.SetActive(false);

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);

        if (gameOverText != null)
        {
            gameOverText.text = reason switch
            {
                GameOverReason.CityDestroyed => MsgCityDestroyed,
                GameOverReason.FalseAlarm    => MsgFalseAlarm,
                _                            => string.Empty
            };
        }
    }

    // ── Coroutine ─────────────────────────────────────────────────────────────

    private IEnumerator ShowDayAnnouncement(int day)
    {
        if (dayText != null)
            dayText.text = $"DAY {day}";

        // Fade in
        yield return StartCoroutine(FadeCanvas(dayCanvasGroup, 0f, 1f, fadeDuration));

        yield return new WaitForSeconds(holdDuration);

        // Fade out
        yield return StartCoroutine(FadeCanvas(dayCanvasGroup, 1f, 0f, fadeDuration));
    }

    private IEnumerator FadeCanvas(CanvasGroup group, float from, float to, float duration)
    {
        if (group == null) yield break;

        float elapsed = 0f;
        group.alpha          = from;
        group.blocksRaycasts = true;

        while (elapsed < duration)
        {
            elapsed      += Time.deltaTime;
            group.alpha   = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        group.alpha          = to;
        group.blocksRaycasts = false;
    }
}
