using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int completedGames = 0;
    [SerializeField] private int interstitialFrequency = 3;

    [SerializeField] private GameObject columnButtons;

    [SerializeField] private float resultPanelDelay = 1.5f;

    public bool IsGameOver { get; private set; }

    public GameMode CurrentGameMode { get; private set; }

    private void Start()
    {
        CurrentGameMode = GameSettings.GameMode;

        AdManager.Instance.HideBanner();

    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    public void SetGameMode(GameMode mode)
    {
        CurrentGameMode = mode;
    }

    public void EndGame(PieceType winner)
    {
        IsGameOver = true;

        columnButtons.SetActive(false);

        StartCoroutine(ShowResultAfterDelay(winner));
    }

    private IEnumerator ShowResultAfterDelay(PieceType winner)
    {
        yield return new WaitForSeconds(resultPanelDelay);

        if (CurrentGameMode == GameMode.HumanVsHuman)
        {
            UIManager.Instance.ShowWinner(winner);
        }
        else
        {
            if (winner == PieceType.Player1)
            {
                UIManager.Instance.ShowVsBotWinner(winner);
            }
            else
            {
                UIManager.Instance.ShowLoser();
            }
        }

        completedGames++;

        if (completedGames % interstitialFrequency == 0)
        {
            AdManager.Instance.ShowInterstitial();
        }
    }

    public void DrawGame()
    {
        IsGameOver = true;

        if (columnButtons != null)
            columnButtons.SetActive(false);

        StartCoroutine(ShowDrawAfterDelay());
    }

    private IEnumerator ShowDrawAfterDelay()
    {
        yield return new WaitForSeconds(resultPanelDelay);

        UIManager.Instance.ShowDraw();
        
        completedGames++;

        if (completedGames % interstitialFrequency == 0)
        {
            AdManager.Instance.ShowInterstitial();
        }
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Home()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ResumeGame()
    {
        IsGameOver = false;

        columnButtons.SetActive(true);

        UIManager.Instance.ShowGameplayUI();

    }

    public void UndoMove()
    {
        if (!BoardManager.Instance.CanUndo())
            return;

        AdManager.Instance.ShowRewardedUndo();
    }
}