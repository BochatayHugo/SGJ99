using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Gère le menu principal. Attach sur un GameObject dans la scène MainMenu.
/// Charge la scène de jeu quand le joueur clique sur Play.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Scene Names")]
    [Tooltip("Nom exact de la scène de jeu principale (dans Build Settings).")]
    [SerializeField] private string gameSceneName = "main";

    [Header("UI")]
    [SerializeField] private Button playButton;

    private void Start()
    {
        // Le jeu peut avoir verrouillé le curseur — on le libère systématiquement.
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);
    }

    /// <summary>Lance la scène de jeu.</summary>
    public void OnPlayClicked()
    {
        SceneManager.LoadScene(gameSceneName);
    }
}
