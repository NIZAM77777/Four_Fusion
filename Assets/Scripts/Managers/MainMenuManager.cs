using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject modeSelectionPanel;
    [SerializeField] private GameObject difficultyPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject combo1Selected;
    [SerializeField] private GameObject combo2Selected;

    private void Start()
    {
        mainMenuPanel.SetActive(true);
        modeSelectionPanel.SetActive(false);
        difficultyPanel.SetActive(false);
        GameSettings.Theme =
    (PieceTheme)PlayerPrefs.GetInt("Theme", 0);

        UpdateComboHighlight();
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

    public void SelectCombo1()
    {
        GameSettings.Theme = PieceTheme.Combo1;

        PlayerPrefs.SetInt("Theme", 0);
        PlayerPrefs.Save();
        UpdateComboHighlight();
    }

    public void SelectCombo2()
    {
        GameSettings.Theme = PieceTheme.Combo2;

        PlayerPrefs.SetInt("Theme", 1);
        PlayerPrefs.Save();
        UpdateComboHighlight();
    }

    private void UpdateComboHighlight()
    {
        combo1Selected.SetActive(false);
        combo2Selected.SetActive(false);

        switch (GameSettings.Theme)
        {
            case PieceTheme.Combo1:
                combo1Selected.SetActive(true);
                break;

            case PieceTheme.Combo2:
                combo2Selected.SetActive(true);
                break;
        }
    }
    public void ExitGame()
    {
        Application.Quit();
    }
}