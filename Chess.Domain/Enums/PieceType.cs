namespace Chess.Domain.Enums
{
    /// <summary>Value of each piece on the board. Each piece is a combination of piece value and color (0 for white, 8 for black)</summary>
    [Flags]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1069:Enums values should not be duplicated", Justification = "<Pending>")]
    public enum PieceType : byte
    {
        /// <summary>No piece</summary>
        None = 0,
        /// <summary>Pawn</summary>
        Pawn = 1,
        /// <summary>Knight</summary>
        Knight = 2,
        /// <summary>Bishop</summary>
        Bishop = 3,
        /// <summary>Rook</summary>
        Rook = 4,
        /// <summary>Queen</summary>
        Queen = 5,
        /// <summary>King</summary>
        King = 6,
        /// <summary>Mask to find the piece</summary>
        PieceMask = 7,
        /// <summary>Piece is black</summary>
        Black = 8,
        /// <summary>White piece</summary>
        White = 0,
    }
}
