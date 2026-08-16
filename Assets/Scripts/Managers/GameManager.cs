using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    private int freeUndoCount = 0;
    private const int HUMAN_VS_HUMAN_FREE_UNDOS = 3;
    private const int HUMAN_VS_AI_FREE_UNDOS = 1;

    private int completedGames = 0;
    [SerializeField] private int interstitialFrequency = 3;

    [SerializeField] private GameObject columnButtons;

    [SerializeField] private float resultPanelDelay = 1.5f;

    public bool IsGameOver { get; private set; }

    public GameMode CurrentGameMode { get; private set; }

    private void Start()
    {
        CurrentGameMode = GameSettings.GameMode;

        freeUndoCount = 0;

        AdManager.Instance.HideBanner();
        BoardManager.Instance.UpdateUndoButton();

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
        if (IsGameOver)
            return;

        if (!BoardManager.Instance.CanUndo())
            return;

        int freeUndoLimit =
            CurrentGameMode == GameMode.HumanVsHuman
            ? HUMAN_VS_HUMAN_FREE_UNDOS
            : HUMAN_VS_AI_FREE_UNDOS;

        if (freeUndoCount < freeUndoLimit)
        {
            // Free undo
            freeUndoCount++;

            BoardManager.Instance.UndoLastTwoMoves();

            return;
        }

        // Free undos finished → rewarded ad
        AdManager.Instance.ShowRewardedUndo();
    }

    public void ResultPanelUndo()
    {
        if (!BoardManager.Instance.CanUndo())
            return;

        AdManager.Instance.ShowRewardedUndo();
    }
}