namespace Chess.Domain.Enums
{
    /// <summary>Result of the current board. Game is finished unless OnGoing or Check</summary>
    public enum GameResult
    {
        /// <summary>Game is going on</summary>
        OnGoing,
        /// <summary>3 times the same board</summary>
        ThreeFoldRepeat,
        /// <summary>50 times without moving a pawn or eating a piece</summary>
        FiftyRuleRepeat,
        /// <summary>No more move for the next player</summary>
        TieNoMove,
        /// <summary>Not enough pieces to do a check mate</summary>
        TieNoMatePossible,
        /// <summary>Check</summary>
        Check,
        /// <summary>Checkmate</summary>
        Mate
    }
}
