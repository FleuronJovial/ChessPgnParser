namespace Chess.Pgn.Parser
{
    public static class PgnUtil
    {
        /// <summary>
        /// Gets Square Id from the PGN representation
        /// </summary>
        /// <param name="move"> PGN square representation.</param>
        /// <returns>
        /// square id (0-63)
        /// PGN representation
        /// </returns>
        public static int GetSquareIdFromPgn(string move)
        {
            int retVal;
            char chr1;
            char chr2;

            if (move.Length != 2)
            {
                retVal = -1;
            }
            else
            {
                chr1 = move.ToLower()[0];
                chr2 = move[1];
                retVal = chr1 < 'a' || chr1 > 'h' || chr2 < '1' || chr2 > '8' ? -1 : 7 - (chr1 - 'a') + ((chr2 - '0') << 3);
            }
            return retVal;
        }

    }
}
