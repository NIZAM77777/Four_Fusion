using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField] private GameObject columnButtons;

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

        if (columnButtons != null)
            columnButtons.SetActive(false);

        StartCoroutine(ShowWinnerAfterDelay(winner));
    }

    private IEnumerator ShowWinnerAfterDelay(PieceType winner)
    {
        yield return new WaitForSeconds(3f);

        UIManager.Instance.ShowWinner(winner);
    }

    public void DrawGame()
    {
        IsGameOver = true;

        if (columnButtons != null)
            columnButtons.SetActive(false);

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