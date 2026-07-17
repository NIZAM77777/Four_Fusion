using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject settingsPanel;

    private void Start()
    {
        mainMenuPanel.SetActive(true);
        modeSelectionPanel.SetActive(false);
        difficultyPanel.SetActive(false);
    }

    public void PlayButton()
    {
        mainMenuPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void HumanVsHuman()
    {
        GameSettings.GameMode = GameMode.HumanVsHuman;

        SceneManager.LoadScene("GameScene");
    }

    public void HumanVsAI()
    {
        GameSettings.GameMode = GameMode.HumanVsAI;

        modeSelectionPanel.SetActive(false);
        difficultyPanel.SetActive(true);
    }

    public void Easy()
    {
        GameSettings.Difficulty = Difficulty.Easy;

        SceneManager.LoadScene("GameScene");
    }

    public void Medium()
    {
        GameSettings.Difficulty = Difficulty.Medium;

        SceneManager.LoadScene("GameScene");
    }

    public void Hard()
    {
        GameSettings.Difficulty = Difficulty.Hard;

        SceneManager.LoadScene("GameScene");
    }

    public void BackToMainMenu()
    {
        modeSelectionPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void BackToModeSelection()
    {
        difficultyPanel.SetActive(false);
        modeSelectionPanel.SetActive(true);
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void ExitGame()
    {
        Application.Quit();
    }
}