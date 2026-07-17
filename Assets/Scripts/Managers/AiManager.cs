using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AIManager : MonoBehaviour
{
    public static AIManager Instance;

    [SerializeField] private BoardManager boardManager;

    [SerializeField] private float thinkingTime = 0.5f;

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

    public void PlayTurn()
    {
        StartCoroutine(PlayTurnRoutine());
    }

    private IEnumerator PlayTurnRoutine()
    {
        yield return new WaitForSeconds(thinkingTime);

        int column = -1;

        switch (GameSettings.Difficulty)
        {
            case Difficulty.Easy:
                column = EasyMove();
                break;

            case Difficulty.Medium:
                column = MediumMove();
                break;

            case Difficulty.Hard:
                column = HardMove();
                break;
        }

        boardManager.DropPiece(column);
    }

    private int EasyMove()
    {
        while (true)
        {
            int randomColumn = Random.Range(0, boardManager.Columns);

            if (!boardManager.IsColumnFull(randomColumn))
                return randomColumn;
        }
    }

    private int FindWinningMove(PieceType player)
    {
        List<int> validMoves = boardManager.GetValidMoves();

        foreach (int column in validMoves)
        {
            if (boardManager.IsWinningMove(column, player))
            {
                return column;
            }
        }

        return -1;
    }

    private int MediumMove()
    {
        int move = FindWinningMove(PieceType.Yellow);

        if (move != -1)
            return move;

        move = FindWinningMove(PieceType.Red);

        if (move != -1)
            return move;

        int center = boardManager.Columns / 2;

        if (boardManager.IsValidMove(center))
            return center;

        return EasyMove();
    }

    private int HardMove()
    {
        return FindBestMove();
    }

    private int Minimax(
     AIBoard board,
     int depth,
     bool maximizingPlayer,
     int alpha,
     int beta)
    {
        if (depth == 0)
        {
            return board.EvaluateBoard();
        }
        int bestScore;

        if (maximizingPlayer)
        {
             bestScore = int.MinValue;

            foreach (int column in board.GetValidMoves())
            {
                board.MakeMove(column, PieceType.Yellow);

                int score = Minimax(
                    board,
                    depth - 1,
                    false,
                    alpha,
                    beta);

                board.UndoMove(column);

                bestScore = Mathf.Max(bestScore, score);

                alpha = Mathf.Max(alpha, bestScore);

                if (alpha >= beta)
                    break;
            }

            return bestScore;
        }

        bestScore = int.MaxValue;

        foreach (int column in board.GetValidMoves())
        {
            board.MakeMove(column, PieceType.Red);

            int score = Minimax(
                board,
                depth - 1,
                true,
                alpha,
                beta);

            board.UndoMove(column);

            bestScore = Mathf.Min(bestScore, score);

            beta = Mathf.Min(beta, bestScore);

            if (alpha >= beta)
                break;
        }

        return bestScore;
    }

    private int FindBestMove()
    {
        PieceType[,] copy =
            boardManager.GetBoardCopy();

        AIBoard board =
            new AIBoard(copy);

        int bestColumn = -1;

        int bestScore = int.MinValue;

        foreach (int column in board.GetValidMoves())
        {
            board.MakeMove(column, PieceType.Yellow);

            int score =
                Minimax(board, 5, false, int.MinValue, int.MaxValue);

            board.UndoMove(column);

            if (score > bestScore)
            {
                bestScore = score;
                bestColumn = column;
            }
        }

        return bestColumn;
    }
}