using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("UI")]
    [SerializeField] private TMP_Text turnText;
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private TMP_Text resultText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        gameOverPanel.SetActive(false);
        UpdateTurn(PieceType.Red);
    }

    public void UpdateTurn(PieceType player)
    {
        turnText.text = player + " Turn";
    }

    public void ShowWinner(PieceType winner)
    {
        gameOverPanel.SetActive(true);
        resultText.text = winner + " Wins!";
    }

    public void ShowDraw()
    {
        gameOverPanel.SetActive(true);
        resultText.text = "Draw!";
    }
}