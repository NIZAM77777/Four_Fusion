using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject difficultyPanel;

    private void Start()
    {
        mainMenuPanel.SetActive(true);
        modeSelectionPanel.SetActive(false);
        difficultyPanel.SetActive(false);
    }

    // PLAY
    public void PlayButton()
    {
        mainMenuPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    // HUMAN VS HUMAN
    public void HumanVsHuman()
    {
        GameSettings.GameMode = GameMode.HumanVsHuman;

        SceneManager.LoadScene("GameScene");
    }

    // HUMAN VS AI
    public void HumanVsAI()
    {
        GameSettings.GameMode = GameMode.HumanVsAI;

        modeSelectionPanel.SetActive(false);
        difficultyPanel.SetActive(true);
    }

    // EASY
    public void Easy()
    {
        GameSettings.Difficulty = Difficulty.Easy;

        SceneManager.LoadScene("GameScene");
    }

    // MEDIUM
    public void Medium()
    {
        GameSettings.Difficulty = Difficulty.Medium;

        SceneManager.LoadScene("GameScene");
    }

    // HARD
    public void Hard()
    {
        GameSettings.Difficulty = Difficulty.Hard;

        SceneManager.LoadScene("GameScene");
    }

    // BACK FROM MODE PANEL
    public void BackToMainMenu()
    {
        modeSelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    // BACK FROM DIFFICULTY PANEL
    public void BackToModeSelection()
    {
        difficultyPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}