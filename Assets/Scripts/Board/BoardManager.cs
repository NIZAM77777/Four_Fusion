using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    [SerializeField] private int columns = 7;
    [SerializeField] private int rows = 6;
    [SerializeField] private float cellSize = 1f;

    [Header("Prefabs")]
    [SerializeField] private BoardCell boardCellPrefab;
    [SerializeField] private Piece redPiecePrefab;
    [SerializeField] private Piece yellowPiecePrefab;

    [Header("Parents")]
    [SerializeField] private Transform piecesParent;

    private BoardCell[,] board;
    private PieceType[,] boardState;
    private Piece[,] spawnedPieces;

    private List<Vector2Int> winningCells = new List<Vector2Int>();

    private PieceType currentPlayer = PieceType.Player1;

    private List<MoveData> moveHistory = new List<MoveData>();

    public static BoardManager Instance;


    private bool isDroppingPiece;

    public PieceType CurrentPlayer => currentPlayer;
    public int Columns => columns;
    public int Rows => rows;
    public bool IsBusy => isDroppingPiece;
    public int Revision { get; private set; }
    public int MoveCount => moveHistory.Count;
    private readonly List<Vector2Int> connectedPieces = new List<Vector2Int>(13);
    private void OnDestroy() { if (Instance == this) Instance = null; }

    private void Start()
    {
        GenerateBoard();

        UpdateUndoButton();

    }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void GenerateBoard()
    {
        board = new BoardCell[columns, rows];
        boardState = new PieceType[columns, rows];
        spawnedPieces = new Piece[columns, rows];

        float boardWidth = (columns - 1) * cellSize;
        float boardHeight = (rows - 1) * cellSize;

        Vector3 offset = new Vector3(
            boardWidth / 2f,
            boardHeight / 2f,
            0f);

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                Vector3 position = new Vector3(
                    column * cellSize,
                    row * cellSize,
                    0f) - offset;

                BoardCell cell = Instantiate(
                    boardCellPrefab,
                    position,
                    Quaternion.identity,
                    transform);

                cell.Column = column;
                cell.Row = row;

                board[column, row] = cell;
            }
        }
    }

    public void DropPiece(int column)
    {
        TryDropPiece(column, false);
    }

    public bool TryDropPiece(int column, bool fromAI)
    {
        if (GameManager.Instance == null || GameManager.Instance.InputBlocked || boardState == null)
            return false;
        bool aiTurn = GameManager.Instance.CurrentGameMode == GameMode.HumanVsAI && currentPlayer == PieceType.Player2;
        if (aiTurn != fromAI || !IsValidMove(column))
            return false;

        if (isDroppingPiece)
            return false;

        int row = GetNextAvailableRow(column);
        if (row == -1)
            return false;

        Piece prefab = currentPlayer == PieceType.Player1 ? redPiecePrefab : yellowPiecePrefab;
        if (prefab == null || board[column, row] == null)
        {
            Debug.LogError("Board piece/cell prefab is missing.");
            return false;
        }
        isDroppingPiece = true;
        Revision++;
        boardState[column, row] = currentPlayer;
        Vector3 targetPosition = board[column, row].transform.position;
        Piece piece = Instantiate(prefab, targetPosition + Vector3.up * 8f, Quaternion.identity, piecesParent);
        piece.PieceType = currentPlayer;
        if (PieceThemeManager.Instance != null)
            piece.SetSprite(currentPlayer == PieceType.Player1 ? PieceThemeManager.Instance.GetPlayer1Sprite() : PieceThemeManager.Instance.GetPlayer2Sprite());
        spawnedPieces[column, row] = piece;
        moveHistory.Add(new MoveData(column, row, currentPlayer, piece));
        piece.MoveTo(targetPosition);
        AudioManager.Instance?.PlayPiecePlace();
        UpdateUndoButton();
        StartCoroutine(WaitForPiece(piece, column, row));
        return true;
    }

    private IEnumerator WaitForPiece(
        Piece piece,
        int column,
        int row)
    {
        while (piece.IsMoving)
        {
            yield return null;
        }


        isDroppingPiece = false;

        if (CheckForWin(column, row))
        {
            HighlightWinningPieces();

            BounceWinningPieces();

            GameManager.Instance.EndGame(currentPlayer);

            yield break;
        }

        if (CheckForDraw())
        {
            GameManager.Instance.DrawGame();

            yield break;
        }

        SwitchPlayer();
    }

    private void SwitchPlayer()
    {
        currentPlayer =
            currentPlayer == PieceType.Player1
            ? PieceType.Player2
            : PieceType.Player1;

        UIManager.Instance.UpdateTurn(currentPlayer);

        UpdateUndoButton();

        if (GameManager.Instance.CurrentGameMode == GameMode.HumanVsAI &&
            currentPlayer == PieceType.Player2)
        {
            AIManager.Instance.PlayTurn();
        }
    }

    public bool IsColumnFull(int column)
    {
        return boardState == null || column < 0 || column >= columns || boardState[column, rows - 1] != PieceType.Empty;
    }

    public int GetNextAvailableRow(int column)
    {
        if (boardState == null || column < 0 || column >= columns) return -1;
        for (int row = 0; row < rows; row++)
        {
            if (boardState[column, row] == PieceType.Empty)
                return row;
        }

        return -1;
    }

    public Piece GetPiece(int column, int row)
    {
        return spawnedPieces[column, row];
    }

    public PieceType GetPieceType(int column, int row)
    {
        return boardState[column, row];
    }
  

    private bool CheckForWin(int column, int row)
    {
        winningCells.Clear();

        if (CheckDirection(column, row, 1, 0))
            return true;

        if (CheckDirection(column, row, 0, 1))
            return true;

        if (CheckDirection(column, row, 1, 1))
            return true;

        if (CheckDirection(column, row, 1, -1))
            return true;

        return false;
    }

    private bool CheckDirection(int column, int row, int xDirection, int yDirection)
    {
        connectedPieces.Clear();

        connectedPieces.Add(new Vector2Int(column, row));

        CollectDirection(
            connectedPieces,
            column,
            row,
            xDirection,
            yDirection);

        CollectDirection(
            connectedPieces,
            column,
            row,
            -xDirection,
            -yDirection);

        if (connectedPieces.Count >= 4)
        {
            winningCells.Clear();
            winningCells.AddRange(connectedPieces);
            return true;
        }

        return false;
    }

    private void CollectDirection(
        List<Vector2Int> connectedPieces,
        int startColumn,
        int startRow,
        int xDirection,
        int yDirection)
    {
        int column = startColumn + xDirection;
        int row = startRow + yDirection;

        while (column >= 0 &&
               column < columns &&
               row >= 0 &&
               row < rows)
        {
            if (boardState[column, row] != currentPlayer)
                break;

            connectedPieces.Add(new Vector2Int(column, row));

            column += xDirection;
            row += yDirection;
        }
    }

  

    private bool CheckForDraw()
    {
        for (int column = 0; column < columns; column++)
        {
            if (!IsColumnFull(column))
                return false;
        }

        return true;
    }

   

    private void HighlightWinningPieces()
    {
        foreach (Vector2Int cell in winningCells)
        {
            Piece piece = spawnedPieces[cell.x, cell.y];

            if (piece != null)
            {
                piece.Highlight();
            }
        }
    }

    private void BounceWinningPieces()
    {
        foreach (Vector2Int cell in winningCells)
        {
            Piece piece = spawnedPieces[cell.x, cell.y];

            if (piece != null)
            {
                piece.Bounce();
            }
        }
    }

    [ContextMenu("Print Board")]
    private void PrintBoard()
    {
        for (int row = rows - 1; row >= 0; row--)
        {
            string line = "";

            for (int column = 0; column < columns; column++)
            {
                switch (boardState[column, row])
                {
                    case PieceType.Empty:
                        line += ". ";
                        break;

                    case PieceType.Player1:
                        line += "R ";
                        break;

                    case PieceType.Player2:
                        line += "Y ";
                        break;
                }
            }

            Debug.Log(line);
        }
    }

    public bool IsValidMove(int column)
    {
        if (column < 0 || column >= columns)
            return false;

        return !IsColumnFull(column);
    }

    public List<int> GetValidMoves()
    {
        List<int> validMoves = new List<int>();

        for (int column = 0; column < columns; column++)
        {
            if (IsValidMove(column))
            {
                validMoves.Add(column);
            }
        }

        return validMoves;
    }

    private void SimulateMove(int column, PieceType player)
    {
        int row = GetNextAvailableRow(column);

        if (row != -1)
        {
            boardState[column, row] = player;
        }
    }

    private void UndoMove(int column)
    {
        for (int row = rows - 1; row >= 0; row--)
        {
            if (boardState[column, row] != PieceType.Empty)
            {
                boardState[column, row] = PieceType.Empty;
                return;
            }
        }
    }
    public PieceType[,] GetBoardCopy()
    {
        PieceType[,] copy = new PieceType[columns, rows];

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                copy[column, row] = boardState[column, row];
            }
        }

        return copy;
    }

    public bool IsWinningMove(int column, PieceType player)
    {
        if (!IsValidMove(column))
            return false;

        int row = GetNextAvailableRow(column);

        SimulateMove(column, player);

        PieceType previousPlayer = currentPlayer;
        currentPlayer = player;

        bool win = CheckForWin(column, row);

        currentPlayer = previousPlayer;

        UndoMove(column);

        return win;
    }

    public void UndoLastTwoMoves()
    {
        if (isDroppingPiece || moveHistory.Count < 2 || SceneTransition.IsLoading)
            return;

        AIManager.Instance?.CancelTurn();
        StopAllCoroutines();
        ResetAllPieceHighlights();
        // After a human winning move, undo it and the prior AI reply, then
        // return to that AI turn. Do not give the human two successive moves.
        PieceType restoredPlayer = moveHistory[moveHistory.Count - 2].Player;

        UndoSingleMove();

        UndoSingleMove();

        currentPlayer = restoredPlayer;
        Revision++;

        UIManager.Instance.UpdateTurn(currentPlayer);

        GameManager.Instance.ResumeGame();

        UpdateUndoButton();
        if (GameManager.Instance.CurrentGameMode == GameMode.HumanVsAI && currentPlayer == PieceType.Player2)
            AIManager.Instance?.PlayTurn();
    }

    private void UndoSingleMove()
    {
        MoveData move = moveHistory[moveHistory.Count - 1];

        moveHistory.RemoveAt(moveHistory.Count - 1);

        boardState[move.Column, move.Row] = PieceType.Empty;

        spawnedPieces[move.Column, move.Row] = null;

        if (move.PieceObject != null)
        {
            Destroy(move.PieceObject.gameObject);
        }
    }

    private void ResetAllPieceHighlights()
    {
        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                Piece piece = spawnedPieces[column, row];

                if (piece != null)
                {
                    piece.ResetHighlight();
                }
            }
        }
    }

    public bool CanUndo()
    {
        if (isDroppingPiece || SceneTransition.IsLoading || GameManager.Instance == null || GameManager.Instance.IsPaused) return false;
        if (moveHistory.Count < 2)
            return false;


        if (GameManager.Instance.CurrentGameMode ==
            GameMode.HumanVsHuman)
        {
            return currentPlayer == PieceType.Player1;
        }


        // Human vs AI
        // Undo is available only during player's turn.
        return currentPlayer == PieceType.Player1;
    }

    public bool CanUndoFromResultPanel()
    {
        return !isDroppingPiece && moveHistory.Count >= 2 && !SceneTransition.IsLoading;
    }

    public void UpdateUndoButton()
    {
        FusionUI.Instance?.RefreshGameplay();
        if (GameManager.Instance == null || UIManager.Instance == null) return;
        if (GameManager.Instance.IsGameOver)
        {
            UIManager.Instance.HideUndoButton();
            return;
        }

        if (CanUndo())
        {
            UIManager.Instance.ShowUndoButton();
        }
        else
        {
            UIManager.Instance.HideUndoButton();
        }
    }
}
