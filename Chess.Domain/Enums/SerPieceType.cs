namespace Chess.Domain.Enums
{
    /// <summary>Same as PieceType, but easier serialization.</summary>
    public enum SerPieceType : byte
    {
        /// <summary>No piece</summary>
        Empty = 0,
        /// <summary>Pawn</summary>
        WhitePawn = 1,
        /// <summary>Knight</summary>
        WhiteKnight = 2,
        /// <summary>Bishop</summary>
        WhiteBishop = 3,
        /// <summary>Rook</summary>
        WhiteRook = 4,
        /// <summary>Queen</summary>
        WhiteQueen = 5,
        /// <summary>King</summary>
        WhiteKing = 6,
        /// <summary>Not used</summary>
        NotUsed1 = 7,
        /// <summary>Not used</summary>
        NotUsed2 = 8,
        /// <summary>Pawn</summary>
        BlackPawn = 9,
        /// <summary>Knight</summary>
        BlackKnight = 10,
        /// <summary>Bishop</summary>
        BlackBishop = 11,
        /// <summary>Rook</summary>
        BlackRook = 12,
        /// <summary>Queen</summary>
        BlackQueen = 13,
        /// <summary>King</summary>
        BlackKing = 14,
        /// <summary>Not used</summary>
        NotUsed3 = 15,
    }

}
