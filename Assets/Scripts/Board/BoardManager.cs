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

    // Stores winning coordinates instead of Piece objects
    private List<Vector2Int> winningCells = new List<Vector2Int>();

    private PieceType currentPlayer = PieceType.Red;

    private bool isDroppingPiece;

    public PieceType CurrentPlayer => currentPlayer;
    public int Columns => columns;
    public int Rows => rows;

    private void Start()
    {
        GenerateBoard();
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
        if (GameManager.Instance.IsGameOver)
            return;

        if (isDroppingPiece)
            return;

        if (IsColumnFull(column))
            return;

        int row = GetNextAvailableRow(column);

        if (row == -1)
            return;

        boardState[column, row] = currentPlayer;

        Piece prefab =
            currentPlayer == PieceType.Red
            ? redPiecePrefab
            : yellowPiecePrefab;

        Vector3 targetPosition =
            board[column, row].transform.position;

        Vector3 spawnPosition =
            targetPosition + Vector3.up * 8f;

        Piece piece = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity,
            piecesParent);

        spawnedPieces[column, row] = piece;

        piece.MoveTo(targetPosition);

        isDroppingPiece = true;

        StartCoroutine(
            WaitForPiece(piece, column, row));
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

        AudioManager.Instance.PlayPieceDrop();

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
            currentPlayer == PieceType.Red
            ? PieceType.Yellow
            : PieceType.Red;

        UIManager.Instance.UpdateTurn(currentPlayer);
    }

    public bool IsColumnFull(int column)
    {
        return boardState[column, rows - 1] != PieceType.Empty;
    }

    public int GetNextAvailableRow(int column)
    {
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
    // ----------------------------
    // WIN DETECTION
    // ----------------------------

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
        List<Vector2Int> connectedPieces = new List<Vector2Int>();

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
            winningCells = connectedPieces;
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

    // ----------------------------
    // DRAW
    // ----------------------------

    private bool CheckForDraw()
    {
        for (int column = 0; column < columns; column++)
        {
            if (!IsColumnFull(column))
                return false;
        }

        return true;
    }

    // ----------------------------
    // WIN EFFECTS
    // ----------------------------

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

                    case PieceType.Red:
                        line += "R ";
                        break;

                    case PieceType.Yellow:
                        line += "Y ";
                        break;
                }
            }

            Debug.Log(line);
        }
    }
}