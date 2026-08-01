using System.Collections.Generic;

public class AIBoard
{
    private PieceType[,] board;

    private int columns;
    private int rows;

    private const int WIN_SCORE = 100000;
    private const int THREE_SCORE = 100;
    private const int TWO_SCORE = 10;

    private const int OPPONENT_THREE_SCORE = -120;
    private const int OPPONENT_TWO_SCORE = -15;

    private const int CENTER_SCORE = 6;

    public AIBoard(PieceType[,] boardState)
    {
        columns = boardState.GetLength(0);
        rows = boardState.GetLength(1);

        board = new PieceType[columns, rows];

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows; row++)
            {
                board[column, row] = boardState[column, row];
            }
        }
    }

    public List<int> GetValidMoves()
    {
        List<int> moves = new List<int>();

        for (int column = 0; column < columns; column++)
        {
            if (board[column, rows - 1] == PieceType.Empty)
            {
                moves.Add(column);
            }
        }

        return moves;
    }

    public int GetNextAvailableRow(int column)
    {
        for (int row = 0; row < rows; row++)
        {
            if (board[column, row] == PieceType.Empty)
            {
                return row;
            }
        }

        return -1;
    }

    public bool MakeMove(int column, PieceType player)
    {
        int row = GetNextAvailableRow(column);

        if (row == -1)
            return false;

        board[column, row] = player;

        return true;
    }

    public void UndoMove(int column)
    {
        for (int row = rows - 1; row >= 0; row--)
        {
            if (board[column, row] != PieceType.Empty)
            {
                board[column, row] = PieceType.Empty;
                return;
            }
        }
    }

    public int EvaluateBoard()
    {
        int score = 0;

        score += EvaluateCenter();

        score += EvaluateHorizontal();

        score += EvaluateVertical();

        score += EvaluateDiagonalPositive();

        score += EvaluateDiagonalNegative();

        return score;
    }

    private int EvaluateCenter()
    {
        int score = 0;

        int center = columns / 2;

        for (int row = 0; row < rows; row++)
        {
            if (board[center, row] == PieceType.Player2)
                score += CENTER_SCORE;

            else if (board[center, row] == PieceType.Player1)
                score -= CENTER_SCORE;
        }

        return score;
    }

    private int EvaluateHorizontal()
    {
        int score = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns - 3; column++)
            {
                score += EvaluateWindow(
                    board[column, row],
                    board[column + 1, row],
                    board[column + 2, row],
                    board[column + 3, row]);
            }
        }

        return score;
    }

    private int EvaluateVertical()
    {
        int score = 0;

        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows - 3; row++)
            {
                score += EvaluateWindow(
                    board[column, row],
                    board[column, row + 1],
                    board[column, row + 2],
                    board[column, row + 3]);
            }
        }

        return score;
    }

    private int EvaluateDiagonalPositive()
    {
        int score = 0;

        for (int column = 0; column < columns - 3; column++)
        {
            for (int row = 0; row < rows - 3; row++)
            {
                score += EvaluateWindow(
                    board[column, row],
                    board[column + 1, row + 1],
                    board[column + 2, row + 2],
                    board[column + 3, row + 3]);
            }
        }

        return score;
    }

    private int EvaluateDiagonalNegative()
    {
        int score = 0;

        for (int column = 0; column < columns - 3; column++)
        {
            for (int row = 3; row < rows; row++)
            {
                score += EvaluateWindow(
                    board[column, row],
                    board[column + 1, row - 1],
                    board[column + 2, row - 2],
                    board[column + 3, row - 3]);
            }
        }

        return score;
    }

    private int EvaluateWindow(
    PieceType a,
    PieceType b,
    PieceType c,
    PieceType d)
    {
        int ai = 0;
        int player = 0;
        int empty = 0;

        PieceType[] window = { a, b, c, d };

        foreach (PieceType piece in window)
        {
            if (piece == PieceType.Player2)
                ai++;

            else if (piece == PieceType.Player1)
                player++;

            else
                empty++;
        }

        if (ai == 4)
            return WIN_SCORE;

        if (player == 4)
            return -WIN_SCORE;

        if (ai == 3 && empty == 1)
            return THREE_SCORE;

        if (ai == 2 && empty == 2)
            return TWO_SCORE;

        if (player == 3 && empty == 1)
            return OPPONENT_THREE_SCORE;

        if (player == 2 && empty == 2)
            return OPPONENT_TWO_SCORE;

        return 0;
    }
}