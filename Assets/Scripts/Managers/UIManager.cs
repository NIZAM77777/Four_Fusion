using UnityEngine;

// Scene-compatible facade: presentation lives in FusionUI, actions in GameManager.
public class UIManager : MonoBehaviour
{
    public static UIManager Instance;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void OnDestroy() { if (Instance == this) Instance = null; }
    private void Start() => UpdateTurn(PieceType.Player1);
    public void ShowUndoButton() => FusionUI.Instance?.RefreshGameplay();
    public void HideUndoButton() => FusionUI.Instance?.RefreshGameplay();
    public void UpdateModeText() => FusionUI.Instance?.RefreshGameplay();
    public void UpdateTurn(PieceType player) => FusionUI.Instance?.RefreshGameplay();
    public void ShowWinner(PieceType winner) => FusionUI.Instance?.ShowResult(winner);
    public void ShowVsBotWinner(PieceType winner) => FusionUI.Instance?.ShowResult(winner);
    public void ShowLoser() => FusionUI.Instance?.ShowResult(PieceType.Player2);
    public void ShowDraw() => FusionUI.Instance?.ShowResult(PieceType.Empty);
    public void ShowGameplayUI() => FusionUI.Instance?.ShowGameplay();
    public void ShowAdUnavailableMessage() => FusionUI.Instance?.Notify("ADS_UNAVAILABLE");
    public void HideAdUnavailableMessage() { }
}
