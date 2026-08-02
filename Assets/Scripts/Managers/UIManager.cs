using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Gameplay UI")]
    [SerializeField] private TMP_Text turnText;

    [Header("Panels")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private GameObject vsBotWinPanel;
    [SerializeField] private GameObject losePanel;
    [SerializeField] private GameObject drawPanel;
    [SerializeField] private GameObject topPanel;
    [SerializeField] private GameObject bottomPanel;

    [Header("Win Panel")]
    [SerializeField] private TMP_Text winnerText;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        drawPanel.SetActive(false);
        vsBotWinPanel.SetActive(false);

        UpdateTurn(PieceType.Player1);
    }

    public void UpdateTurn(PieceType player)
    {
        turnText.text = player + " Turn";
    }

    public void ShowWinner(PieceType winner)
    {
        HideAllPanels();

        winPanel.SetActive(true);

        if (winner == PieceType.Player1)
            winnerText.text = "1";
        else
            winnerText.text = "2";
    }
    public void ShowVsBotWinner(PieceType winner)
    {
        HideAllPanels();

        vsBotWinPanel.SetActive(true);

        //if (winner == PieceType.Player1)
        //    winnerText.text = "You";
        //else
        //    winnerText.text = "Bot";
    }

    public void ShowLoser()
    {
        HideAllPanels();

        losePanel.SetActive(true);
    }

    public void ShowDraw()
    {
        HideAllPanels();

        drawPanel.SetActive(true);
    }

    private void HideAllPanels()
    {
        winPanel.SetActive(false);
        losePanel.SetActive(false);
        drawPanel.SetActive(false);
        topPanel.SetActive(false);
        bottomPanel.SetActive(false);
    }
}