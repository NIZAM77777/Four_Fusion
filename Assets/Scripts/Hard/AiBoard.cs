using System.Collections.Generic;

public class AIBoard
{
    private PieceType[,] board;

    private int columns;
    private int rows;

    private const int WIN_SCORE = 100000;
    private const int THREE_SCORE = 600;
    private const int TWO_SCORE = 80;

    private const int OPPONENT_THREE_SCORE = -700;
    private const int OPPONENT_TWO_SCORE = -100;

    private const int CENTER_SCORE = 25;

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

        score += EvaluateForks();

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

        // Winning positions
        if (ai == 4)
            return WIN_SCORE;

        if (player == 4)
            return -WIN_SCORE;

        // AI attacking
        if (ai == 3 && empty == 1)
            return 1200;

        if (ai == 2 && empty == 2)
            return 150;

        if (ai == 1 && empty == 3)
            return 15;

        // Player attacking
        if (player == 3 && empty == 1)
            return -1500;

        if (player == 2 && empty == 2)
            return -180;

        if (player == 1 && empty == 3)
            return -20;

        return 0;
    }

    private int EvaluateForks()
    {
        int score = 0;

        foreach (int column in GetValidMoves())
        {
            // AI fork
            MakeMove(column, PieceType.Player2);

            int aiWinningMoves = CountWinningMoves(PieceType.Player2);

            UndoMove(column);

            if (aiWinningMoves >= 2)
            {
                score += 2500;
            }

            // Player fork
            MakeMove(column, PieceType.Player1);

            int playerWinningMoves = CountWinningMoves(PieceType.Player1);

            UndoMove(column);

            if (playerWinningMoves >= 2)
            {
                score -= 3000;
            }
        }

        return score;
    }

    private int CountWinningMoves(PieceType player)
    {
        int count = 0;

        foreach (int column in GetValidMoves())
        {
            MakeMove(column, player);

            if (CheckWinner(player))
            {
                count++;
            }

            UndoMove(column);
        }

        return count;
    }

    private bool CheckWinner(PieceType player)
    {
        // Horizontal
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns - 3; column++)
            {
                if (board[column, row] == player &&
                    board[column + 1, row] == player &&
                    board[column + 2, row] == player &&
                    board[column + 3, row] == player)
                    return true;
            }
        }

        // Vertical
        for (int column = 0; column < columns; column++)
        {
            for (int row = 0; row < rows - 3; row++)
            {
                if (board[column, row] == player &&
                    board[column, row + 1] == player &&
                    board[column, row + 2] == player &&
                    board[column, row + 3] == player)
                    return true;
            }
        }

        // Positive diagonal
        for (int column = 0; column < columns - 3; column++)
        {
            for (int row = 0; row < rows - 3; row++)
            {
                if (board[column, row] == player &&
                    board[column + 1, row + 1] == player &&
                    board[column + 2, row + 2] == player &&
                    board[column + 3, row + 3] == player)
                    return true;
            }
        }

        // Negative diagonal
        for (int column = 0; column < columns - 3; column++)
        {
            for (int row = 3; row < rows; row++)
            {
                if (board[column, row] == player &&
                    board[column + 1, row - 1] == player &&
                    board[column + 2, row - 2] == player &&
                    board[column + 3, row - 3] == player)
                    return true;
            }
        }

        return false;
    }



    public bool IsValidMove(int column)
    {
        if (column < 0 || column >= columns)
            return false;

        return board[column, rows - 1] == PieceType.Empty;
    }

}