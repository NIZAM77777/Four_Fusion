using System;
using System.Threading;

// Pure managed search. Never reads Unity objects from the worker thread.
public static class AISearch
{
    private static readonly int[] Order = { 3, 2, 4, 1, 5, 0, 6 };
    public static int FindBestMove(PieceType[,] state, CancellationToken cancellation, int depth = 8)
    {
        var board = new AIBoard(state);
        int best = -1, score = int.MinValue;
        foreach (int column in Order)
        {
            cancellation.ThrowIfCancellationRequested();
            if (!board.IsValidMove(column)) continue;
            board.MakeMove(column, PieceType.Player2);
            int value = Search(board, depth - 1, false, int.MinValue + 1, int.MaxValue, cancellation);
            board.UndoMove(column);
            if (value > score) { score = value; best = column; }
        }
        return best;
    }
    private static int Search(AIBoard board, int depth, bool maximize, int alpha, int beta, CancellationToken cancellation)
    {
        cancellation.ThrowIfCancellationRequested();
        if (board.CheckWinner(PieceType.Player2)) return 1000000 + depth;
        if (board.CheckWinner(PieceType.Player1)) return -1000000 - depth;
        if (!board.HasValidMoves()) return 0;
        if (depth <= 0) return board.EvaluateBoard();
        int best = maximize ? int.MinValue : int.MaxValue;
        foreach (int column in Order)
        {
            if (!board.IsValidMove(column)) continue;
            board.MakeMove(column, maximize ? PieceType.Player2 : PieceType.Player1);
            int value = Search(board, depth - 1, !maximize, alpha, beta, cancellation);
            board.UndoMove(column);
            best = maximize ? Math.Max(best, value) : Math.Min(best, value);
            if (maximize) alpha = Math.Max(alpha, best); else beta = Math.Min(beta, best);
            if (alpha >= beta) break;
        }
        return best;
    }
}
