namespace Chess.Domain.Enums
{
    /// <summary>Any repetition causing a draw?</summary>
    public enum RepeatResult
    {
        /// <summary>No repetition found</summary>
        NoRepeat,
        /// <summary>3 times the same board</summary>
        ThreeFoldRepeat,
        /// <summary>50 times without moving a pawn or eating a piece</summary>
        FiftyRuleRepeat
    };
}
