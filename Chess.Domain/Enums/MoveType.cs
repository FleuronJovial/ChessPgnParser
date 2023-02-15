namespace Chess.Domain.Enums
{
    /// <summary>Type of possible move</summary>
    public enum MoveType : byte
    {
        /// <summary>Normal move</summary>
        Normal = 0,
        /// <summary>Pawn which is promoted to a queen</summary>
        PawnPromotionToQueen = 1,
        /// <summary>Castling</summary>
        Castle = 2,
        /// <summary>Prise en passant</summary>
        EnPassant = 3,
        /// <summary>Pawn which is promoted to a rook</summary>
        PawnPromotionToRook = 4,
        /// <summary>Pawn which is promoted to a bishop</summary>
        PawnPromotionToBishop = 5,
        /// <summary>Pawn which is promoted to a knight</summary>
        PawnPromotionToKnight = 6,
        /// <summary>Piece type mask</summary>
        MoveTypeMask = 15,
        /// <summary>The move eat a piece</summary>
        PieceEaten = 16,
        /// <summary>Move coming from book opening</summary>
        MoveFromBook = 32
    }
}
