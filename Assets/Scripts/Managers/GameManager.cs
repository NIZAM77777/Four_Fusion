using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [SerializeField]
    private GameObject columnButtons;

    public bool IsGameOver { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void EndGame(PieceType winner)
    {
        IsGameOver = true;
        columnButtons.SetActive(false);
        AudioManager.Instance.PlayVictory();
        UIManager.Instance.ShowWinner(winner);
    }

    public void DrawGame()
    {
        IsGameOver = true;
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