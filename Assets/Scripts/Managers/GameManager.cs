using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject columnButtons;

    [SerializeField] private float resultPanelDelay = 1.5f;

    public bool IsGameOver { get; private set; }

    public GameMode CurrentGameMode { get; private set; }

    private void Start()
    {
        CurrentGameMode = GameSettings.GameMode;
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
            // Human vs Bot
            if (winner == PieceType.Player1)
            {
                // You won -> new Vs Bot Win Panel
                UIManager.Instance.ShowVsBotWinner(winner);
            }
            else
            {
                UIManager.Instance.ShowLoser();
            }
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
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Home()
    {
        SceneManager.LoadScene("MainMenu");
    }
}