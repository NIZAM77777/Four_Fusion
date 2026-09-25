using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    private int freeUndoCount;
    private static int completedGames;
    [SerializeField] private int interstitialFrequency = 3;
    [SerializeField] private GameObject columnButtons;
    [SerializeField] private float resultPanelDelay = 1.1f;
    [SerializeField] private float interstitialDelay = 1f;
    public bool IsGameOver { get; private set; }
    public bool IsPaused { get; private set; }
    public bool InputBlocked => IsGameOver || IsPaused || SceneTransition.IsLoading || (AdManager.Instance != null && AdManager.Instance.IsShowingAd);
    public GameMode CurrentGameMode { get; private set; }
    public int FreeUndosRemaining => Mathf.Max(0, (CurrentGameMode == GameMode.HumanVsHuman ? 3 : 1) - freeUndoCount);
    private bool countedResult;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { Instance = null; completedGames = 0; }
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        CurrentGameMode = GameSettings.GameMode;
    }
    private void Start() { AdManager.Instance?.HideBanner(); BoardManager.Instance?.UpdateUndoButton(); }
    private void OnDestroy() { if (Instance == this) Instance = null; Time.timeScale = 1; }
    public void SetGameMode(GameMode mode) => CurrentGameMode = mode;
    public void SetPaused(bool value)
    {
        IsPaused = value;
        Time.timeScale = value ? 0 : 1;
        FusionUI.Instance?.RefreshGameplay();
    }
    private void OnApplicationPause(bool paused)
    {
        if (paused && !IsGameOver && (AdManager.Instance == null || !AdManager.Instance.IsShowingAd)) SetPaused(true);
    }
    public void EndGame(PieceType winner) { if (!IsGameOver) StartResult(winner); }
    public void DrawGame() { if (!IsGameOver) StartResult(PieceType.Empty); }
    private void StartResult(PieceType winner)
    {
        IsGameOver = true;
        if (columnButtons != null) columnButtons.SetActive(false);
        BoardManager.Instance?.UpdateUndoButton();
        StartCoroutine(ShowResult(winner));
    }
    private IEnumerator ShowResult(PieceType winner)
    {
        yield return new WaitForSeconds(resultPanelDelay);
        if (winner == PieceType.Empty) { UIManager.Instance?.ShowDraw(); AudioManager.Instance?.PlayDraw(); }
        else if (CurrentGameMode == GameMode.HumanVsHuman) { UIManager.Instance?.ShowWinner(winner); AudioManager.Instance?.PlayWin(); }
        else if (winner == PieceType.Player1) { UIManager.Instance?.ShowVsBotWinner(winner); AudioManager.Instance?.PlayWin(); }
        else { UIManager.Instance?.ShowLoser(); AudioManager.Instance?.PlayLose(); }
        AdManager.Instance?.ShowBanner();
        if (!countedResult)
        {
            countedResult = true;
            completedGames++;
            if (completedGames % Mathf.Max(3, interstitialFrequency) == 0)
            {
                yield return new WaitForSeconds(interstitialDelay);
                if (IsGameOver) AdManager.Instance?.ShowInterstitial();
            }
        }
    }
    public void RestartGame() { if (SceneTransition.IsLoading || (AdManager.Instance != null && AdManager.Instance.IsShowingAd)) return; SetPaused(false); SceneTransition.Load(SceneManager.GetActiveScene().name); }
    public void Home() { if (SceneTransition.IsLoading || (AdManager.Instance != null && AdManager.Instance.IsShowingAd)) return; SetPaused(false); SceneTransition.Load("MainMenu"); }
    public void ResumeGame()
    {
        StopAllCoroutines();
        IsGameOver = false;
        if (columnButtons != null) columnButtons.SetActive(true);
        AdManager.Instance?.HideBanner();
        UIManager.Instance?.ShowGameplayUI();
    }
    public void UndoMove()
    {
        if (InputBlocked || BoardManager.Instance == null || !BoardManager.Instance.CanUndo()) return;
        if (FreeUndosRemaining > 0) { freeUndoCount++; BoardManager.Instance.UndoLastTwoMoves(); }
        else if (AdManager.Instance != null) AdManager.Instance.ShowRewardedUndo();
        else UIManager.Instance?.ShowAdUnavailableMessage();
    }
    public void ResultPanelUndo()
    {
        if (!IsGameOver || SceneTransition.IsLoading || BoardManager.Instance == null || !BoardManager.Instance.CanUndoFromResultPanel()) return;
        if (AdManager.Instance != null) AdManager.Instance.ShowRewardedResultPanelUndo();
        else UIManager.Instance?.ShowAdUnavailableMessage();
    }
}
