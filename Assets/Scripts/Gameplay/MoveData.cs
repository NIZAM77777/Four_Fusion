using UnityEngine;

[System.Serializable]
public class MoveData
{
    public int Column;
    public int Row;
    public PieceType Player;
    public Piece PieceObject;

    public MoveData(int column, int row, PieceType player, Piece pieceObject)
    {
        Column = column;
        Row = row;
        Player = player;
        PieceObject = pieceObject;
    }
}