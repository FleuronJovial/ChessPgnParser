namespace Chess.Domain.Enums
{
    /// <summary>Mask for board extra info</summary>
    [Flags]
    public enum BoardStateMask
    {
        /// <summary>0-63 to express the EnPassant possible position</summary>
        EnPassant = 63,
        /// <summary>black player is next to move</summary>
        BlackToMove = 64,
        /// <summary>white left castling is possible</summary>
        WLCastling = 128,
        /// <summary>white right castling is possible</summary>
        WRCastling = 256,
        /// <summary>black left castling is possible</summary>
        BLCastling = 512,
        /// <summary>black right castling is possible</summary>
        BRCastling = 1024,
        /// <summary>Mask use to save the number of times the board has been repeated</summary>
        BoardRepMask = 2048 + 4096 + 8192
    };

}
