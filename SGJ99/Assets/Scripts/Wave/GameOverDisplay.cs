using UnityEngine;
using TMPro;

/// <summary>
/// Listens for game over events and displays the appropriate message on a UI canvas.
/// Attach to a dedicated GameOver canvas GameObject.
/// </summary>
public class GameOverDisplay : MonoBehaviour
{
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private TMP_Text   gameOverText;

    private static readonly string MsgCityDestroyed     = "TOUTE LA VILLE A ÉTÉ DÉCIMÉE.\nVous avez échoué.";
    private static readonly string MsgFalseAlarm        = "FAUSSE ALERTE.\nVous avez été renvoyé.";
    private static readonly string MsgEvacuationSuccess = "ÉVACUATION RÉUSSIE.\nBien joué.";
    private static readonly string MsgSuccess           = "PROTECTION ACTIVÉE.\nLa ville est sauvée.";

    private void OnEnable()
    {
        WaveGameManager.OnGameOver += OnGameOver;
    }

    private void OnDisable()
    {
        WaveGameManager.OnGameOver -= OnGameOver;
    }

    private void Start()
    {
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
    }

    private void OnGameOver(GameOverReason reason)
    {
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(true);

        if (gameOverText == null) return;

        gameOverText.text = reason switch
        {
            GameOverReason.CityDestroyed      => MsgCityDestroyed,
            GameOverReason.FalseAlarm         => MsgFalseAlarm,
            GameOverReason.EvacuationSuccess  => MsgEvacuationSuccess,
            GameOverReason.Success            => MsgSuccess,
            _                                 => string.Empty
        };
    }
}
