using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    //==================================================
    // UNDO SETTINGS
    //==================================================

    private int freeUndoCount = 0;

    private const int HUMAN_VS_HUMAN_FREE_UNDOS = 3;

    private const int HUMAN_VS_AI_FREE_UNDOS = 1;


    //==================================================
    // INTERSTITIAL SETTINGS
    //==================================================

    private int completedGames = 0;

    // Show interstitial after every completed game
    [SerializeField] private int interstitialFrequency = 1;


    //==================================================
    // UI
    //==================================================

    [SerializeField] private GameObject columnButtons;

    [SerializeField] private float resultPanelDelay = 1.1f;
    [SerializeField] private float interstitialDelay = 1f;


    //==================================================
    // GAME STATE
    //==================================================

    public bool IsGameOver { get; private set; }

    public GameMode CurrentGameMode { get; private set; }


    //==================================================
    // UNITY
    //==================================================

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }


    private void Start()
    {
        CurrentGameMode = GameSettings.GameMode;

        // Reset free undo count for this new game
        freeUndoCount = 0;

        // Gameplay does not show banner
        AdManager.Instance.HideBanner();

        BoardManager.Instance.UpdateUndoButton();

        AudioManager.Instance.StopMusic();
    }


    //==================================================
    // GAME MODE
    //==================================================

    public void SetGameMode(GameMode mode)
    {
        CurrentGameMode = mode;
    }


    //==================================================
    // WIN
    //==================================================

    public void EndGame(PieceType winner)
    {
        IsGameOver = true;

        if (columnButtons != null)
        {
            columnButtons.SetActive(false);
        }

        StartCoroutine(
            ShowResultAfterDelay(winner));
    }


    private IEnumerator ShowResultAfterDelay(
        PieceType winner)
    {
        yield return new WaitForSeconds(
            resultPanelDelay);


        // Show correct result panel
        if (CurrentGameMode ==
            GameMode.HumanVsHuman)
        {
            UIManager.Instance.ShowWinner(winner);

            AudioManager.Instance.PlayWin();
        }
        else
        {
            if (winner == PieceType.Player1)
            {
                UIManager.Instance.ShowVsBotWinner(
                    winner);

                AudioManager.Instance.PlayWin();
            }
            else
            {
                UIManager.Instance.ShowLoser();

                AudioManager.Instance.PlayLose();
            }
        }


        AdManager.Instance.ShowBanner();

        completedGames++;

        if (completedGames %
            interstitialFrequency == 0)
        {
            yield return new WaitForSeconds(interstitialDelay);

            AdManager.Instance.ShowInterstitial();
        }
    }


    //==================================================
    // DRAW
    //==================================================

    public void DrawGame()
    {
        IsGameOver = true;

        if (columnButtons != null)
        {
            columnButtons.SetActive(false);
        }

        StartCoroutine(
            ShowDrawAfterDelay());
    }


    private IEnumerator ShowDrawAfterDelay()
    {
        yield return new WaitForSeconds(
            resultPanelDelay);


        UIManager.Instance.ShowDraw();


        // Show banner on draw screen
        AdManager.Instance.ShowBanner();


        completedGames++;

        if (completedGames %
            interstitialFrequency == 0)
        {
            yield return new WaitForSeconds(interstitialDelay);

            AdManager.Instance.ShowInterstitial();
        }

        AudioManager.Instance.PlayLose();
    }


    //==================================================
    // RESTART
    //==================================================

    public void RestartGame()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().buildIndex);
    }


    //==================================================
    // HOME
    //==================================================

    public void Home()
    {
        AudioManager.Instance.PlayMusic();

        SceneManager.LoadScene("MainMenu");
    }


    //==================================================
    // RESUME AFTER UNDO
    //==================================================

    public void ResumeGame()
    {
        IsGameOver = false;


        if (columnButtons != null)
        {
            columnButtons.SetActive(true);
        }


        // Gameplay screen should not have banner
        AdManager.Instance.HideBanner();


        UIManager.Instance.ShowGameplayUI();
    }


    //==================================================
    // NORMAL GAMEPLAY UNDO
    //==================================================

    public void UndoMove()
    {
        // Cannot undo if game is over
        if (IsGameOver)
            return;


        // Cannot undo if board doesn't allow it
        if (!BoardManager.Instance.CanUndo())
            return;


        int freeUndoLimit =
            CurrentGameMode ==
            GameMode.HumanVsHuman
            ? HUMAN_VS_HUMAN_FREE_UNDOS
            : HUMAN_VS_AI_FREE_UNDOS;


        //==================================================
        // FREE UNDO
        //==================================================

        if (freeUndoCount <
            freeUndoLimit)
        {
            freeUndoCount++;

            BoardManager.Instance
                .UndoLastTwoMoves();

            return;
        }


        //==================================================
        // REWARDED UNDO
        //==================================================

        AdManager.Instance.ShowRewardedUndo();
    }


    //==================================================
    // RESULT PANEL UNDO
    //==================================================

    public void ResultPanelUndo()
    {
        if (!BoardManager.Instance
            .CanUndoFromResultPanel())
        {
            return;
        }

        AdManager.Instance.ShowRewardedResultPanelUndo();
    }
}
