using System.Collections;
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

    private PieceType currentPlayer = PieceType.Red;

    private bool isDroppingPiece = false;

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
            0f
        );

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                Vector3 position = new Vector3(
                    column * cellSize,
                    row * cellSize,
                    0f
                ) - offset;

                BoardCell cell = Instantiate(
                    boardCellPrefab,
                    position,
                    Quaternion.identity,
                    transform
                );

                cell.Column = column;
                cell.Row = row;

                board[column, row] = cell;
            }
        }
    }

    public void DropPiece(int column)
    {
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

        Vector3 targetPosition = board[column, row].transform.position;

        Vector3 spawnPosition = targetPosition + Vector3.up * 8f;

        Piece piece = Instantiate(
            prefab,
            spawnPosition,
            Quaternion.identity,
            piecesParent
        );

        spawnedPieces[column, row] = piece;

        piece.MoveTo(targetPosition);

        isDroppingPiece = true;

        StartCoroutine(WaitForPiece(piece));
    }

    private IEnumerator WaitForPiece(Piece piece)
    {
        while (piece.IsMoving)
        {
            yield return null;
        }

        isDroppingPiece = false;

        SwitchPlayer();
    }

    private void SwitchPlayer()
    {
        currentPlayer = currentPlayer == PieceType.Red
            ? PieceType.Yellow
            : PieceType.Red;
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

    public PieceType GetPieceType(int column, int row)
    {
        return boardState[column, row];
    }

    public Piece GetPiece(int column, int row)
    {
        return spawnedPieces[column, row];
    }
}