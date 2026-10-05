using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    private const int HardSearchMilliseconds = 550;
    public static AIManager Instance;
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private float thinkingTime = .35f;
    private Coroutine turn;
    private CancellationTokenSource cancellation;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }
    private void OnDisable() => CancelTurn();
    private void OnDestroy() { CancelTurn(); if (Instance == this) Instance = null; }
    public void CancelTurn()
    {
        if (turn != null) StopCoroutine(turn);
        turn = null;
        cancellation?.Cancel();
        cancellation?.Dispose();
        cancellation = null;
    }
    public void PlayTurn()
    {
        if (turn != null || boardManager == null || boardManager.CurrentPlayer != PieceType.Player2) return;
        turn = StartCoroutine(PlayTurnRoutine());
    }
    private IEnumerator PlayTurnRoutine()
    {
        int revision = boardManager.Revision;
        yield return new WaitForSeconds(Mathf.Min(thinkingTime, .6f));
        int column;
        if (GameSettings.Difficulty == Difficulty.Hard)
        {
            cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            var snapshot = boardManager.GetBoardCopy();
            var search = Task.Run(() => AISearch.FindBestMoveTimed(snapshot, token, 8, HardSearchMilliseconds), token);
            while (!search.IsCompleted) yield return null;
            if (search.IsCanceled) { turn = null; yield break; }
            if (search.IsFaulted) { Debug.LogException(search.Exception); column = MediumMove(); }
            else column = search.Result;
            cancellation.Dispose();
            cancellation = null;
        }
        else column = GameSettings.Difficulty == Difficulty.Medium ? MediumMove() : EasyMove();
        while (GameManager.Instance != null && (GameManager.Instance.IsPaused || (AdManager.Instance != null && AdManager.Instance.IsShowingAd))) yield return null;
        turn = null;
        if (boardManager != null && boardManager.Revision == revision && column >= 0)
            boardManager.TryDropPiece(column, true);
    }
    private int EasyMove()
    {
        int selected = -1, count = 0;
        for (int column = 0; column < boardManager.Columns; column++)
            if (boardManager.IsValidMove(column) && Random.Range(0, ++count) == 0) selected = column;
        return selected;
    }
    private int FindWinningMove(PieceType player)
    {
        for (int column = 0; column < boardManager.Columns; column++)
            if (boardManager.IsWinningMove(column, player)) return column;
        return -1;
    }
    private int MediumMove()
    {
        int move = FindWinningMove(PieceType.Player2);
        if (move >= 0) return move;
        move = FindWinningMove(PieceType.Player1);
        if (move >= 0) return move;
        int center = boardManager.Columns / 2;
        return boardManager.IsValidMove(center) ? center : EasyMove();
    }
}
