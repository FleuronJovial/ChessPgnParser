using Chess.Domain.Enums;

namespace Chess.Domain
{
    public interface IChessBoard
    {
        PlayerColor CurrentPlayer { get; }
        bool IsDesignMode { get; }
        long ZobristKey { get; }

        ChessBoard? Clone();
        bool CloseDesignMode(PlayerColor startingColor, BoardStateMask boardMask, int enPassantPos);
        GameResult DoMove(MoveExt move);
        List<Move> EnumMoveList(PlayerColor playerColor);
        void OpenDesignMode();
        void ResetBoard();
        BoardStateMask ComputeBoardExtraInfo(bool addRepetitionInfo);

        PieceType this[int pos] { get; set; }
    }
    public class ChessBoard : IChessBoard
    {
        /// <summary>Chess board</summary>
        ///     A  B  C  D  E  F  G  H
        ///   ---------------------------
        /// 8 | 63 62 61 60 59 58 57 56 | 8
        /// 7 | 55 54 53 52 51 50 49 48 | 7
        /// 6 | 47 46 45 44 43 42 41 40 | 6
        /// 5 | 39 38 37 36 35 34 33 32 | 5
        /// 4 | 31 30 29 28 27 26 25 24 | 4
        /// 3 | 23 22 21 20 19 18 17 16 | 3
        /// 2 | 15 14 13 12 11 10 9  8  | 2
        /// 1 | 7  6  5  4  3  2  1  0  | 1
        ///   ---------------------------
        ///     A  B  C  D  E  F  G  H
        private readonly PieceType[] m_board;
        /// <summary>Position of the black king</summary>
        internal int m_blackKingPos;
        /// <summary>Position of the white king</summary>
        private int m_whiteKingPos;
        /// <summary>Number of pieces of each kind/color</summary>

        /// <summary>Number of pieces of each kind/color</summary>
        private readonly int[] m_pieceTypeCount;

        /// <summary>Number of time the right black rook has been moved. Used to determine if castle is possible</summary>
        private int m_rightBlackRookMoveCount;
        /// <summary>Number of time the left black rook has been moved. Used to determine if castle is possible</summary>
        private int m_leftBlackRookMoveCount;
        /// <summary>Number of time the black king has been moved. Used to determine if castle is possible</summary>
        private int m_blackKingMoveCount;
        /// <summary>Number of time the right white rook has been moved. Used to determine if castle is possible</summary>
        private int m_rightWhiteRookMoveCount;
        /// <summary>Number of time the left white rook has been moved. Used to determine if castle is possible</summary>
        private int m_leftWhiteRookMoveCount;
        /// <summary>Number of time the white king has been moved. Used to determine if castle is possible</summary>
        private int m_whiteKingMoveCount;
        /// <summary>White has castle if true</summary>
        private bool m_isWhiteCastled;
        /// <summary>Black has castle if true</summary>
        private bool m_isBlackCastled;
        /// <summary>Position behind the pawn which had just been moved from 2 positions</summary>
        private int m_possibleEnPassantPos;
        /// <summary>Stack of m_iPossibleEnPassantAt values</summary>
        private Stack<int> m_pPossibleEnPassantPosStack;

        /// <summary>Information about pieces attack</summary>
        private AttackPosInfo m_posInfo;

        /// <summary>NULL position info</summary>
        public static readonly AttackPosInfo sAttackPosInfoNull = new(0, 0);
        /// <summary>Possible diagonal or linear moves for each board position</summary>
        private static readonly int[][][] s_caseMoveDiagLine;
        /// <summary>Possible diagonal moves for each board position</summary>
        private static readonly int[][][] s_caseMoveDiagonal;
        /// <summary>Possible linear moves for each board position</summary>
        private static readonly int[][][] s_caseMoveLine;
        /// <summary>Possible knight moves for each board position</summary>
        private static readonly int[][] s_caseMoveKnight;
        /// <summary>Possible king moves for each board position</summary>
        private static readonly int[][] s_caseMoveKing;
        /// <summary>Possible board positions a black pawn can attack from each board position</summary>
        private static readonly int[][] s_caseBlackPawnCanAttackFrom;
        /// <summary>Possible board positions a white pawn can attack from each board position</summary>
        private static readonly int[][] s_caseWhitePawnCanAttackFrom;

        /// <summary>
        /// Get a piece at the specified position. Position 0 = Lower right (H1), 63 = Higher left (A8)
        /// </summary>
        /// <param name="pos">  Index position. 0 = Lower right (H1), 63 = Higher left (A8)</param>
        public PieceType this[int pos]
        {
            get => m_board[pos];
            set
            {
                if (IsDesignMode)
                {
                    if (m_board[pos] != value)
                    {
                        m_pieceTypeCount[(int)m_board[pos]]--;
                        m_board[pos] = value;
                        m_pieceTypeCount[(int)m_board[pos]]++;
                    }
                }
                else
                {
                    throw new NotSupportedException("Cannot be used if not in design mode");
                }
            }
        }


        /// <summary>
        /// Player which play next
        /// </summary>
        public PlayerColor CurrentPlayer { get; private set; }

        public bool IsDesignMode { get; private set; }

        public long ZobristKey { get; private set; }

        /// <summary>
        /// Stack of all moves done since initial board
        /// </summary>
        public MovePosStack MovePosStack { get; private set; }

        /// <summary>
        /// true if the board is standard, false if initialized from design mode or FEN
        /// </summary>
        public bool IsStdInitialBoard { get; private set; }

        /// <summary>
        /// Get the move history which handle the fifty-move rule and the threefold repetition rule
        /// </summary>
        public MoveHistory MoveHistory { get; private set; }


        /// <summary>
        /// Class static constructor. 
        /// Builds the list of possible moves for each piece type per position.
        /// Etablished the value of each type of piece for board evaluation.
        /// </summary>
        static ChessBoard()
        {
            sAttackPosInfoNull.PiecesAttacked = 0;
            sAttackPosInfoNull.PiecesDefending = 0;
            s_caseMoveDiagLine = new int[64][][];
            s_caseMoveDiagonal = new int[64][][];
            s_caseMoveLine = new int[64][][];
            s_caseMoveKnight = new int[64][];
            s_caseMoveKing = new int[64][];
            s_caseWhitePawnCanAttackFrom = new int[64][];
            s_caseBlackPawnCanAttackFrom = new int[64][];
            for (int i = 0; i < 64; i++)
            {
                s_caseMoveDiagLine[i] = GetAccessibleSquares(i,
                                                                       deltas: new int[] { -1, -1, -1, 0, -1, 1, 0, -1, 0, 1, 1, -1, 1, 0, 1, 1 },
                                                                       canBeRepeat: true);
                s_caseMoveDiagonal[i] = GetAccessibleSquares(i,
                                                                       deltas: new int[] { -1, -1, -1, 1, 1, -1, 1, 1 },
                                                                       canBeRepeat: true);
                s_caseMoveLine[i] = GetAccessibleSquares(i,
                                                                       deltas: new int[] { -1, 0, 1, 0, 0, -1, 0, 1 },
                                                                       canBeRepeat: true);
                s_caseMoveKnight[i] = GetAccessibleSquares(i,
                                                                       deltas: new int[] { 1, 2, 1, -2, 2, -1, 2, 1, -1, 2, -1, -2, -2, -1, -2, 1 },
                                                                       canBeRepeat: false)[0];
                s_caseMoveKing[i] = GetAccessibleSquares(i,
                                                                       deltas: new int[] { -1, -1, -1, 0, -1, 1, 0, -1, 0, 1, 1, -1, 1, 0, 1, 1 },
                                                                       canBeRepeat: false)[0];
                s_caseWhitePawnCanAttackFrom[i] = GetAccessibleSquares(i,
                                                                       deltas: new int[] { -1, -1, 1, -1 },
                                                                       canBeRepeat: false)[0];
                s_caseBlackPawnCanAttackFrom[i] = GetAccessibleSquares(i,
                                                                       deltas: new int[] { -1, 1, 1, 1 },
                                                                       canBeRepeat: false)[0];
            }
        }

        /// <summary>
        /// Get all squares which can be access by a piece positioned at squarePos
        /// </summary>
        /// <param name="squarePos">   Square position of the piece</param>
        /// <param name="deltas">      Array of delta (in tuple) used to list the accessible position</param>
        /// <param name="canBeRepeat"> True for Queen, Rook and Bishop. False for Knight, King and Pawn</param>
        private static int[][] GetAccessibleSquares(int squarePos, int[] deltas, bool canBeRepeat)
        {
            List<int[]> retVal = new(4);
            int colPos;
            int rowPos;
            int colIndex;
            int rowIndex;
            int colDelta;
            int rowDelta;
            int posOfs;
            int newPos;
            List<int> lineSquares;

            retVal.Clear();
            lineSquares = new List<int>(8);
            colPos = squarePos & 7;
            rowPos = squarePos >> 3;
            for (int i = 0; i < deltas.Length; i += 2)
            {
                colDelta = deltas[i];
                rowDelta = deltas[i + 1];
                posOfs = (rowDelta << 3) + colDelta;
                colIndex = colPos + colDelta;
                rowIndex = rowPos + rowDelta;
                newPos = squarePos + posOfs;
                if (canBeRepeat)
                {
                    lineSquares.Clear();
                    while (colIndex >= 0 && colIndex < 8 && rowIndex >= 0 && rowIndex < 8)
                    {
                        lineSquares.Add(newPos);
                        colIndex += colDelta;
                        rowIndex += rowDelta;
                        newPos += posOfs;
                    }
                    if (lineSquares.Count != 0)
                    {
                        retVal.Add(lineSquares.ToArray());
                    }
                }
                else if (colIndex >= 0 && colIndex < 8 && rowIndex >= 0 && rowIndex < 8)
                {
                    lineSquares.Add(newPos);
                }
            }
            if (!canBeRepeat)
            {
                retVal.Add(lineSquares.ToArray());
            }
            return retVal.ToArray();
        }

        /// <summary>
        /// Class constructor. Build a board.
        /// </summary>
        public ChessBoard(/*ISearchTrace<Move>? trace, Dispatcher dispatcher*/)
        {
            m_board = new PieceType[64];
            m_pieceTypeCount = new int[16];
            //m_book = null;
            //m_rnd = new Random((int)DateTime.Now.Ticks);
            //m_repRnd = new Random(0);
            m_pPossibleEnPassantPosStack = new Stack<int>(256);
            //m_trace = trace;
            MoveHistory = new MoveHistory();
            IsDesignMode = false;
            MovePosStack = new MovePosStack();
            //  m_boardAdaptor = new ChessGameBoardAdaptor(this, dispatcher);
            ResetBoard();
        }

        /// <summary>
        /// Class constructor. Use to create a new clone
        /// </summary>
        /// <param name="chessBoard">   Board to copy from</param>
        private ChessBoard(ChessBoard chessBoard) : this(/*chessBoard.m_boardAdaptor.Dispatcher*/)
        {
            CopyFrom(chessBoard);
        }

        /// <summary>
        /// Copy the state of the board from the specified one.
        /// </summary>
        /// <param name="chessBoard"> Board to copy from</param>
        public void CopyFrom(ChessBoard chessBoard)
        {
            int[] arr;

            chessBoard.m_board.CopyTo(m_board, 0);
            chessBoard.m_pieceTypeCount.CopyTo(m_pieceTypeCount, 0);
            arr = chessBoard.m_pPossibleEnPassantPosStack.ToArray();
            Array.Reverse(arr);
            m_pPossibleEnPassantPosStack = new Stack<int>(arr);
            // m_book = chessBoard.m_book;
            m_blackKingPos = chessBoard.m_blackKingPos;
            m_whiteKingPos = chessBoard.m_whiteKingPos;
            // m_rnd = chessBoard.m_rnd;
            //m_repRnd = chessBoard.m_repRnd;
            m_rightBlackRookMoveCount = chessBoard.m_rightBlackRookMoveCount;
            m_leftBlackRookMoveCount = chessBoard.m_leftBlackRookMoveCount;
            m_blackKingMoveCount = chessBoard.m_blackKingMoveCount;
            m_rightWhiteRookMoveCount = chessBoard.m_rightWhiteRookMoveCount;
            m_leftWhiteRookMoveCount = chessBoard.m_leftWhiteRookMoveCount;
            m_whiteKingMoveCount = chessBoard.m_whiteKingMoveCount;
            m_isWhiteCastled = chessBoard.m_isWhiteCastled;
            m_isBlackCastled = chessBoard.m_isBlackCastled;
            m_possibleEnPassantPos = chessBoard.m_possibleEnPassantPos;
            ZobristKey = chessBoard.ZobristKey;
            //  m_trace = chessBoard.m_trace;
            MovePosStack = chessBoard.MovePosStack.Clone();
            MoveHistory = chessBoard.MoveHistory.Clone();
            CurrentPlayer = chessBoard.CurrentPlayer;
        }

        /// <summary>
        /// Clone the current board
        /// </summary>
        /// <returns>
        /// New copy of the board
        /// </returns>
        public ChessBoard Clone()
        {
            return new(this);
        }


        /// <summary>
        /// Try to close the design mode.
        /// </summary>
        /// <param name="startingColor"> Color of the next move</param>
        /// <param name="boardMask">     Board extra information</param>
        /// <param name="enPassantPos">  Position of en passant or 0 if none</param>
        /// <returns>
        /// true if succeed, false if board is invalid
        /// </returns>

        public bool CloseDesignMode(PlayerColor startingColor, BoardStateMask boardMask, int enPassantPos)
        {
            bool retVal;

            if (!IsDesignMode)
            {
                retVal = true;
            }
            else
            {
                ResetInitialBoardInfo(startingColor, false, boardMask, enPassantPos);
                retVal = m_pieceTypeCount[(int)(PieceType.King | PieceType.White)] == 1 &&
                    m_pieceTypeCount[(int)(PieceType.King | PieceType.Black)] == 1;
            }
            return retVal;
        }


        /// <summary>
        /// Do the move
        /// </summary>
        /// <param name="move"> Move to do</param>
        /// <returns>
        /// NoRepeat        No repetition
        /// ThreeFoldRepeat Three times the same board
        /// FiftyRuleRepeat Fifty moves without pawn move or piece eaten
        /// </returns>
        public GameResult DoMove(MoveExt move)
        {
            GameResult retVal;
            RepeatResult repeatResult;

            repeatResult = DoMoveNoLog(move.Move);
            retVal = GetCurrentResult(repeatResult);
            MovePosStack.AddMove(move);
            return retVal;
        }

        /// <summary>
        /// Gets the current board result
        /// </summary>
        /// <returns>
        /// NoRepeat Move is possible
        /// Check    Move is possible, but the user is currently in check
        /// Tie      Move is not possible, no move for the user
        /// Mate     Move is not possible, user is checkmate
        /// </returns>
        public GameResult GetCurrentResult(RepeatResult repeatResult)
        {
            GameResult retVal;
            List<Move> moveList;
            PlayerColor playerColor;

            switch (repeatResult)
            {
                case RepeatResult.ThreeFoldRepeat:
                    retVal = GameResult.ThreeFoldRepeat;
                    break;
                case RepeatResult.FiftyRuleRepeat:
                    retVal = GameResult.FiftyRuleRepeat;
                    break;
                default:
                    playerColor = CurrentPlayer;
                    moveList = EnumMoveList(playerColor);
                    retVal = IsCheck(playerColor)
                        ? (moveList.Count == 0) ? GameResult.Mate : GameResult.Check
                        : IsEnoughPieceForCheckMate() ? (moveList.Count == 0) ? GameResult.TieNoMove : GameResult.OnGoing : GameResult.TieNoMatePossible;
                    break;
            }
            return retVal;
        }

        /// <summary>
        /// Check if there is enough pieces to make a check mate
        /// </summary>
        /// <returns>
        /// true            Yes
        /// false           No
        /// </returns>
        public bool IsEnoughPieceForCheckMate()
        {
            bool retVal;
            int bigPieceCount;
            int whiteBishop;
            int blackBishop;
            int whiteKnight;
            int blackKnight;

            if (m_pieceTypeCount[(int)(PieceType.Pawn | PieceType.White)] != 0 ||
                 m_pieceTypeCount[(int)(PieceType.Pawn | PieceType.Black)] != 0)
            {
                retVal = true;
            }
            else
            {
                bigPieceCount = m_pieceTypeCount[(int)(PieceType.Queen | PieceType.White)] +
                                m_pieceTypeCount[(int)(PieceType.Queen | PieceType.Black)] +
                                m_pieceTypeCount[(int)(PieceType.Rook | PieceType.White)] +
                                m_pieceTypeCount[(int)(PieceType.Rook | PieceType.Black)];
                if (bigPieceCount != 0)
                {
                    retVal = true;
                }
                else
                {
                    whiteBishop = m_pieceTypeCount[(int)(PieceType.Bishop | PieceType.White)];
                    blackBishop = m_pieceTypeCount[(int)(PieceType.Bishop | PieceType.Black)];
                    whiteKnight = m_pieceTypeCount[(int)(PieceType.Knight | PieceType.White)];
                    blackKnight = m_pieceTypeCount[(int)(PieceType.Knight | PieceType.Black)];
                    if ((whiteBishop + whiteKnight) >= 2 || (blackBishop + blackKnight) >= 2)
                    {
                        // Two knights is typically impossible... but who knows!
                        retVal = true;
                    }
                    else
                    {
                        retVal = false;
                    }
                }
            }
            return retVal;
        }

        /// <summary>
        /// Do the move (without log)
        /// </summary>
        /// <param name="move"> Move to do</param>
        /// <returns>
        /// NoRepeat        No repetition
        /// ThreeFoldRepeat Three times the same board
        /// FiftyRuleRepeat Fifty moves without pawn move or piece eaten
        /// </returns>
        public RepeatResult DoMoveNoLog(Move move)
        {
            RepeatResult retVal;
            PieceType pieceType;
            PieceType oldPieceType;
            int enPassantVictimPos;
            int delta;
            bool isPawnMoveOrPieceEaten;

            m_pPossibleEnPassantPosStack.Push(m_possibleEnPassantPos);
            m_possibleEnPassantPos = 0;
            pieceType = m_board[move.StartPos];
            isPawnMoveOrPieceEaten = ((pieceType & PieceType.PieceMask) == PieceType.Pawn) |
                                     ((move.Type & MoveType.PieceEaten) == MoveType.PieceEaten);
            switch (move.Type & MoveType.MoveTypeMask)
            {
                case MoveType.Castle:
                    UpdatePackedBoardAndZobristKey(move.EndPos, pieceType, move.StartPos, PieceType.None);
                    m_board[move.EndPos] = pieceType;
                    m_board[move.StartPos] = PieceType.None;
                    if ((pieceType & PieceType.Black) != 0)
                    {
                        if (move.EndPos == 57)
                        {
                            UpdatePackedBoardAndZobristKey(58, m_board[56], 56, PieceType.None);
                            m_board[58] = m_board[56];
                            m_board[56] = PieceType.None;
                        }
                        else
                        {
                            UpdatePackedBoardAndZobristKey(60, m_board[63], 63, PieceType.None);
                            m_board[60] = m_board[63];
                            m_board[63] = PieceType.None;
                        }
                        m_isBlackCastled = true;
                        m_blackKingPos = move.EndPos;
                    }
                    else
                    {
                        if (move.EndPos == 1)
                        {
                            UpdatePackedBoardAndZobristKey(2, m_board[0], 0, PieceType.None);
                            m_board[2] = m_board[0];
                            m_board[0] = PieceType.None;
                        }
                        else
                        {
                            UpdatePackedBoardAndZobristKey(4, m_board[7], 7, PieceType.None);
                            m_board[4] = m_board[7];
                            m_board[7] = PieceType.None;
                        }
                        m_isWhiteCastled = true;
                        m_whiteKingPos = move.EndPos;
                    }
                    break;
                case MoveType.EnPassant:
                    UpdatePackedBoardAndZobristKey(move.EndPos, pieceType, move.StartPos, PieceType.None);
                    m_board[move.EndPos] = pieceType;
                    m_board[move.StartPos] = PieceType.None;
                    enPassantVictimPos = (move.StartPos & 56) + (move.EndPos & 7);
                    oldPieceType = m_board[enPassantVictimPos];
                    UpdatePackedBoardAndZobristKey(enPassantVictimPos, PieceType.None);
                    m_board[enPassantVictimPos] = PieceType.None;
                    m_pieceTypeCount[(int)oldPieceType]--;
                    break;
                default:
                    // PawnPromotion To or normal moves
                    oldPieceType = m_board[move.EndPos];
                    switch (move.Type & MoveType.MoveTypeMask)
                    {
                        case MoveType.PawnPromotionToQueen:
                            m_pieceTypeCount[(int)pieceType]--;
                            pieceType = PieceType.Queen | (pieceType & PieceType.Black);
                            m_pieceTypeCount[(int)pieceType]++;
                            break;
                        case MoveType.PawnPromotionToRook:
                            m_pieceTypeCount[(int)pieceType]--;
                            pieceType = PieceType.Rook | (pieceType & PieceType.Black);
                            m_pieceTypeCount[(int)pieceType]++;
                            break;
                        case MoveType.PawnPromotionToBishop:
                            m_pieceTypeCount[(int)pieceType]--;
                            pieceType = PieceType.Bishop | (pieceType & PieceType.Black);
                            m_pieceTypeCount[(int)pieceType]++;
                            break;
                        case MoveType.PawnPromotionToKnight:
                            m_pieceTypeCount[(int)pieceType]--;
                            pieceType = PieceType.Knight | (pieceType & PieceType.Black);
                            m_pieceTypeCount[(int)pieceType]++;
                            break;
                        default:
                            break;
                    }
                    UpdatePackedBoardAndZobristKey(move.EndPos, pieceType, move.StartPos, PieceType.None);
                    m_board[move.EndPos] = pieceType;
                    m_board[move.StartPos] = PieceType.None;
                    m_pieceTypeCount[(int)oldPieceType]--;
                    switch (pieceType)
                    {
                        case PieceType.King | PieceType.Black:
                            m_blackKingPos = move.EndPos;
                            if (move.StartPos == 59)
                            {
                                m_blackKingMoveCount++;
                            }
                            break;
                        case PieceType.King | PieceType.White:
                            m_whiteKingPos = move.EndPos;
                            if (move.StartPos == 3)
                            {
                                m_whiteKingMoveCount++;
                            }
                            break;
                        case PieceType.Rook | PieceType.Black:
                            if (move.StartPos == 56)
                            {
                                m_leftBlackRookMoveCount++;
                            }
                            else if (move.StartPos == 63)
                            {
                                m_rightBlackRookMoveCount++;
                            }
                            break;
                        case PieceType.Rook | PieceType.White:
                            if (move.StartPos == 0)
                            {
                                m_leftWhiteRookMoveCount++;
                            }
                            else if (move.StartPos == 7)
                            {
                                m_rightWhiteRookMoveCount++;
                            }
                            break;
                        case PieceType.Pawn | PieceType.White:
                        case PieceType.Pawn | PieceType.Black:
                            delta = move.StartPos - move.EndPos;
                            if (delta is (-16) or 16)
                            {
                                m_possibleEnPassantPos = move.EndPos + (delta >> 1); // Position behind the pawn
                            }
                            break;
                    }
                    break;
            }
            MoveHistory.UpdateCurrentPackedBoard(ComputeBoardExtraInfo(addRepetitionInfo: false));
            retVal = MoveHistory.AddCurrentPackedBoard(ZobristKey, isPawnMoveOrPieceEaten);
            CurrentPlayer = CurrentPlayer == PlayerColor.White ? PlayerColor.Black : PlayerColor.White;
            return retVal;
        }

        /// <summary>
        /// Update the packed board representation and the value of the hash key representing the current board state.
        /// </summary>
        /// <param name="chgPos">   Position of the change</param>
        /// <param name="newPiece"> New piece</param>
        private void UpdatePackedBoardAndZobristKey(int chgPos, PieceType newPiece)
        {
            ZobristKey = ZobristKeyUtil.UpdateZobristKey(ZobristKey, chgPos, m_board[chgPos], newPiece);
            MoveHistory.UpdateCurrentPackedBoard(chgPos, newPiece);
        }

        /// <summary>
        /// Update the packed board representation and the value of the hash key representing the current board state. Use if two
        /// board positions are changed.
        /// </summary>
        /// <param name="pos1">      Position of the change</param>
        /// <param name="newPiece1"> New piece</param>
        /// <param name="pos2">      Position of the change</param>
        /// <param name="newPiece2"> New piece</param>
        private void UpdatePackedBoardAndZobristKey(int pos1, PieceType newPiece1, int pos2, PieceType newPiece2)
        {
            ZobristKey = ZobristKeyUtil.UpdateZobristKey(ZobristKey, pos1, m_board[pos1], newPiece1, pos2, m_board[pos2], newPiece2);
            MoveHistory.UpdateCurrentPackedBoard(pos1, newPiece1);
            MoveHistory.UpdateCurrentPackedBoard(pos2, newPiece2);
        }


        /// <summary>
        /// Enumerates all the possible moves for a player
        /// </summary>
        /// <param name="playerColor"> Color doing the the move</param>
        /// <returns>
        /// List of possible moves
        /// </returns>
        public List<Move> EnumMoveList(PlayerColor playerColor)
        {
            return EnumMoveList(playerColor, true, out _)!;
        }

        /// <summary>
        /// Enumerates the attacking position using an array of possible position and one possible enemy piece
        /// </summary>
        /// <param name="attackPosList"> List to fill with the attacking position. Can be null if only the count is wanted</param>
        /// <param name="caseMoveList">  Array of position.</param>
        /// <param name="pieceType">     Piece which can possibly attack this position</param>
        /// <returns>
        /// Count of attacker
        /// </returns>
        private int EnumTheseAttackPos(List<byte>? attackPosList, int[] caseMoveList, PieceType pieceType)
        {
            int retVal = 0;

            foreach (int newPos in caseMoveList)
            {
                if (m_board[newPos] == pieceType)
                {
                    retVal++;
                    attackPosList?.Add((byte)newPos);
                }
            }
            return retVal;
        }

        /// <summary>
        /// Enumerates the attacking position using arrays of possible position and two possible enemy pieces
        /// </summary>
        /// <param name="attackPosList"> List to fill with the attacking position. Can be null if only the count is wanted</param>
        /// <param name="caseMoveList">  List of array of position</param>
        /// <param name="pieceType1">    Piece which can possibly attack this position</param>
        /// <param name="pieceType2">    Piece which can possibly attack this position</param>
        /// <returns>
        /// Count of attacker
        /// </returns>
        private int EnumTheseAttackPos(List<byte>? attackPosList, int[][] caseMoveList, PieceType pieceType1, PieceType pieceType2)
        {
            int retVal = 0;
            PieceType pieceType;

            foreach (int[] moveList in caseMoveList)
            {
                foreach (int newPos in moveList)
                {
                    pieceType = m_board[newPos];
                    if (pieceType != PieceType.None)
                    {
                        if (pieceType == pieceType1 ||
                            pieceType == pieceType2)
                        {
                            retVal++;
                            attackPosList?.Add((byte)newPos);
                        }
                        break;
                    }
                }
            }
            return retVal;
        }


        /// <summary>
        /// Enumerates all position which can attack a given position
        /// </summary>
        /// <param name="playerColor">   Position to check for black or white player</param>
        /// <param name="pos">           Position to check.</param>
        /// <param name="attackPosList"> Array to fill with the attacking position. Can be null if only the count is wanted</param>
        /// <returns>
        /// Count of attacker
        /// </returns>
        private int EnumAttackPos(PlayerColor playerColor, int pos, List<byte>? attackPosList)
        {
            int retVal;
            PieceType pieceColor;
            PieceType enemyQueen;
            PieceType enemyRook;
            PieceType enemyKing;
            PieceType enemyBishop;
            PieceType enemyKnight;
            PieceType enemyPawn;

            pieceColor = (playerColor == PlayerColor.Black) ? PieceType.White : PieceType.Black;
            enemyQueen = PieceType.Queen | pieceColor;
            enemyRook = PieceType.Rook | pieceColor;
            enemyKing = PieceType.King | pieceColor;
            enemyBishop = PieceType.Bishop | pieceColor;
            enemyKnight = PieceType.Knight | pieceColor;
            enemyPawn = PieceType.Pawn | pieceColor;
            retVal = EnumTheseAttackPos(attackPosList, s_caseMoveDiagonal[pos], enemyQueen, enemyBishop);
            retVal += EnumTheseAttackPos(attackPosList, s_caseMoveLine[pos], enemyQueen, enemyRook);
            retVal += EnumTheseAttackPos(attackPosList, s_caseMoveKing[pos], enemyKing);
            retVal += EnumTheseAttackPos(attackPosList, s_caseMoveKnight[pos], enemyKnight);
            retVal += EnumTheseAttackPos(attackPosList,
                                             (playerColor == PlayerColor.Black) ? s_caseWhitePawnCanAttackFrom[pos] : s_caseBlackPawnCanAttackFrom[pos],
                                             enemyPawn);
            return retVal;
        }

        /// <summary>
        /// Enumerates the castling move
        /// </summary>
        /// <param name="playerColor"> Color doing the the move</param>
        /// <param name="movePosList"> List of moves</param>
        private void EnumCastleMove(PlayerColor playerColor, List<Move>? movePosList)
        {
            if (playerColor == PlayerColor.Black)
            {
                if (!m_isBlackCastled)
                {
                    if (m_blackKingMoveCount == 0)
                    {
                        if (m_leftBlackRookMoveCount == 0 &&
                            m_board[57] == PieceType.None &&
                            m_board[58] == PieceType.None &&
                            m_board[56] == (PieceType.Rook | PieceType.Black))
                        {
                            if (EnumAttackPos(playerColor, 58, null) == 0 &&
                                EnumAttackPos(playerColor, 59, null) == 0)
                            {
                                AddIfNotCheck(playerColor, 59, 57, MoveType.Castle, movePosList);
                            }
                        }
                        if (m_rightBlackRookMoveCount == 0 &&
                            m_board[60] == PieceType.None &&
                            m_board[61] == PieceType.None &&
                            m_board[62] == PieceType.None &&
                            m_board[63] == (PieceType.Rook | PieceType.Black))
                        {
                            if (EnumAttackPos(playerColor, 59, null) == 0 &&
                                EnumAttackPos(playerColor, 60, null) == 0)
                            {
                                AddIfNotCheck(playerColor, 59, 61, MoveType.Castle, movePosList);
                            }
                        }
                    }
                }
            }
            else
            {
                if (!m_isWhiteCastled)
                {
                    if (m_whiteKingMoveCount == 0)
                    {
                        if (m_leftWhiteRookMoveCount == 0 &&
                            m_board[1] == PieceType.None &&
                            m_board[2] == PieceType.None &&
                            m_board[0] == (PieceType.Rook | PieceType.White))
                        {
                            if (EnumAttackPos(playerColor, 2, null) == 0 &&
                                EnumAttackPos(playerColor, 3, null) == 0)
                            {
                                AddIfNotCheck(playerColor, 3, 1, MoveType.Castle, movePosList);
                            }
                        }
                        if (m_rightWhiteRookMoveCount == 0 &&
                            m_board[4] == PieceType.None &&
                            m_board[5] == PieceType.None &&
                            m_board[6] == PieceType.None &&
                            m_board[7] == (PieceType.Rook | PieceType.White))
                        {
                            if (EnumAttackPos(playerColor, 3, null) == 0 &&
                                EnumAttackPos(playerColor, 4, null) == 0)
                            {
                                AddIfNotCheck(playerColor, 3, 5, MoveType.Castle, movePosList);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determine if the specified king is attacked
        /// </summary>
        /// <param name="playerColor"> King's color to check</param>
        /// <param name="kingPos">     Position of the king</param>
        /// <returns>
        /// true if in check
        /// </returns>
        private bool IsCheck(PlayerColor playerColor, int kingPos)
        {
            return EnumAttackPos(playerColor, kingPos, null) != 0;
        }

        /// <summary>
        /// Determine if the specified king is attacked
        /// </summary>
        /// <param name="playerColor"> King's color to check</param>
        /// <returns>
        /// true if in check
        /// </returns>
        public bool IsCheck(PlayerColor playerColor)
        {
            return IsCheck(playerColor, (playerColor == PlayerColor.Black) ? m_blackKingPos : m_whiteKingPos);
        }

        /// <summary>
        /// Add a move to the move list if the move doesn't provokes the king to be attacked.
        /// </summary>
        /// <param name="playerColor"> Color doing the the move</param>
        /// <param name="startPos">    Starting position</param>
        /// <param name="endPos">      Ending position</param>
        /// <param name="moveType">    Type of the move</param>
        /// <param name="movePosList"> List of moves</param>
        private void AddIfNotCheck(PlayerColor playerColor, int startPos, int endPos, MoveType moveType, List<Move>? movePosList)
        {
            PieceType newPiece;
            PieceType oldPiece;
            Move move;
            bool isCheck;

            oldPiece = m_board[endPos];
            newPiece = m_board[startPos];
            m_board[endPos] = newPiece;
            m_board[startPos] = PieceType.None;
            isCheck = ((newPiece & PieceType.PieceMask) == PieceType.King) ? IsCheck(playerColor, endPos) : IsCheck(playerColor);
            m_board[startPos] = m_board[endPos];
            m_board[endPos] = oldPiece;
            if (!isCheck)
            {
                move.OriginalPiece = m_board[endPos];
                move.StartPos = (byte)startPos;
                move.EndPos = (byte)endPos;
                move.Type = moveType;
                if (m_board[endPos] != PieceType.None || moveType == MoveType.EnPassant)
                {
                    move.Type |= MoveType.PieceEaten;
                    m_posInfo.PiecesAttacked++;
                }
                movePosList?.Add(move);
            }
        }

        /// <summary>
        /// Add a pawn promotion series of moves to the move list if the move doesn't provokes the king to be attacked.
        /// </summary>
        /// <param name="playerColor"> Color doing the the move</param>
        /// <param name="startPos">    Starting position</param>
        /// <param name="endPos">      Ending position</param>
        /// <param name="listMovePos"> List of moves</param>
        private void AddPawnPromotionIfNotCheck(PlayerColor playerColor, int startPos, int endPos, List<Move>? listMovePos)
        {
            AddIfNotCheck(playerColor, startPos, endPos, MoveType.PawnPromotionToQueen, listMovePos);
            AddIfNotCheck(playerColor, startPos, endPos, MoveType.PawnPromotionToRook, listMovePos);
            AddIfNotCheck(playerColor, startPos, endPos, MoveType.PawnPromotionToBishop, listMovePos);
            AddIfNotCheck(playerColor, startPos, endPos, MoveType.PawnPromotionToKnight, listMovePos);
        }

        /// <summary>
        /// Add a move to the move list if the new position is empty or is an enemy
        /// </summary>
        /// <param name="playerColor"> Color doing the the move</param>
        /// <param name="startPos">    Starting position</param>
        /// <param name="endPos">      Ending position</param>
        /// <param name="listMovePos"> List of moves</param>
        private bool AddMoveIfEnemyOrEmpty(PlayerColor playerColor, int startPos, int endPos, List<Move>? listMovePos)
        {
            bool retVal;
            PieceType oldPiece;

            retVal = m_board[endPos] == PieceType.None;
            oldPiece = m_board[endPos];
            if (retVal || (oldPiece & PieceType.Black) != 0 != (playerColor == PlayerColor.Black))
            {
                AddIfNotCheck(playerColor, startPos, endPos, MoveType.Normal, listMovePos);
            }
            else
            {
                m_posInfo.PiecesDefending++;
            }
            return retVal;
        }

        /// <summary>
        /// Enumerates the move a specified pawn can do
        /// </summary>
        /// <param name="playerColor">  Color doing the the move</param>
        /// <param name="startPos">     Pawn position</param>
        /// <param name="movePosList">  List of moves</param>
        private void EnumPawnMove(PlayerColor playerColor, int startPos, List<Move>? movePosList)
        {
            int dir;
            int newPos;
            int newColPos;
            int rowPos;
            bool canMove2Case;

            rowPos = startPos >> 3;
            canMove2Case = (playerColor == PlayerColor.Black) ? (rowPos == 6) : (rowPos == 1);
            dir = (playerColor == PlayerColor.Black) ? -8 : 8;
            newPos = startPos + dir;
            if (newPos is >= 0 and < 64)
            {
                if (m_board[newPos] == PieceType.None)
                {
                    rowPos = newPos >> 3;
                    if (rowPos is 0 or 7)
                    {
                        AddPawnPromotionIfNotCheck(playerColor, startPos, newPos, movePosList);
                    }
                    else
                    {
                        AddIfNotCheck(playerColor, startPos, newPos, MoveType.Normal, movePosList);
                    }
                    if (canMove2Case && m_board[newPos + dir] == PieceType.None)
                    {
                        AddIfNotCheck(playerColor, startPos, newPos + dir, MoveType.Normal, movePosList);
                    }
                }
            }
            newPos = startPos + dir;
            if (newPos is >= 0 and < 64)
            {
                newColPos = newPos & 7;
                rowPos = newPos >> 3;
                if (newColPos != 0 && m_board[newPos - 1] != PieceType.None)
                {
                    if ((m_board[newPos - 1] & PieceType.Black) == 0 == (playerColor == PlayerColor.Black))
                    {
                        if (rowPos is 0 or 7)
                        {
                            AddPawnPromotionIfNotCheck(playerColor, startPos, newPos - 1, movePosList);
                        }
                        else
                        {
                            AddIfNotCheck(playerColor, startPos, newPos - 1, MoveType.Normal, movePosList);
                        }
                    }
                    else
                    {
                        m_posInfo.PiecesDefending++;
                    }
                }
                if (newColPos != 7 && m_board[newPos + 1] != PieceType.None)
                {
                    if ((m_board[newPos + 1] & PieceType.Black) == 0 == (playerColor == PlayerColor.Black))
                    {
                        if (rowPos is 0 or 7)
                        {
                            AddPawnPromotionIfNotCheck(playerColor, startPos, newPos + 1, movePosList);
                        }
                        else
                        {
                            AddIfNotCheck(playerColor, startPos, newPos + 1, MoveType.Normal, movePosList);
                        }
                    }
                    else
                    {
                        m_posInfo.PiecesDefending++;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the en passant move
        /// </summary>
        /// <param name="playerColor">  Color doing the the move</param>
        /// <param name="movePosList">  List of moves</param>
        private void EnumEnPassant(PlayerColor playerColor, List<Move>? movePosList)
        {
            int colPos;
            PieceType attackingPawn;
            PieceType pawnInDanger;
            int posBehindPawn;
            int posPawnInDanger;

            if (m_possibleEnPassantPos != 0)
            {
                posBehindPawn = m_possibleEnPassantPos;
                if (playerColor == PlayerColor.White)
                {
                    posPawnInDanger = posBehindPawn - 8;
                    attackingPawn = PieceType.Pawn | PieceType.White;
                }
                else
                {
                    posPawnInDanger = posBehindPawn + 8;
                    attackingPawn = PieceType.Pawn | PieceType.Black;
                }
                pawnInDanger = m_board[posPawnInDanger];
                // Check if there is an attacking pawn at the left
                colPos = posPawnInDanger & 7;
                if (colPos > 0 && m_board[posPawnInDanger - 1] == attackingPawn)
                {
                    m_board[posPawnInDanger] = PieceType.None;
                    AddIfNotCheck(playerColor,
                                  posPawnInDanger - 1,
                                  posBehindPawn,
                                  MoveType.EnPassant,
                                  movePosList);
                    m_board[posPawnInDanger] = pawnInDanger;
                }
                if (colPos < 7 && m_board[posPawnInDanger + 1] == attackingPawn)
                {
                    m_board[posPawnInDanger] = PieceType.None;
                    AddIfNotCheck(playerColor,
                                  posPawnInDanger + 1,
                                  posBehindPawn,
                                  MoveType.EnPassant,
                                  movePosList);
                    m_board[posPawnInDanger] = pawnInDanger;
                }
            }
        }

        /// <summary>
        /// Enumerates the move a specified piece can do using the pre-compute move array
        /// </summary>
        /// <param name="playerColor">         Color doing the the move</param>
        /// <param name="startPos">            Starting position</param>
        /// <param name="moveListForThisCase"> List of array of possible moves</param>
        /// <param name="listMovePos">         List of moves</param>
        private void EnumFromArray(PlayerColor playerColor, int startPos, int[][] moveListForThisCase, List<Move>? listMovePos)
        {
            foreach (int[] movePosForThisDiag in moveListForThisCase)
            {
                foreach (int newPos in movePosForThisDiag)
                {
                    if (!AddMoveIfEnemyOrEmpty(playerColor, startPos, newPos, listMovePos))
                    {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Enumerates the move a specified piece can do using the pre-compute move array
        /// </summary>
        /// <param name="playerColor">         Color doing the the move</param>
        /// <param name="startPos">            Starting position</param>
        /// <param name="moveListForThisCase"> Array of possible moves</param>
        /// <param name="listMovePos">         List of moves</param>
        private void EnumFromArray(PlayerColor playerColor, int startPos, int[] moveListForThisCase, List<Move>? listMovePos)
        {
            foreach (int newPos in moveListForThisCase)
            {
                _ = AddMoveIfEnemyOrEmpty(playerColor, startPos, newPos, listMovePos);
            }
        }

        /// <summary>
        /// Enumerates all the possible moves for a player
        /// </summary>
        /// <param name="playerColor">      Color doing the the move</param>
        /// <param name="isMoveListNeeded"> true to returns a MoveList</param>
        /// <param name="attackPosInfo">    Structure to fill with pieces information</param>
        /// <returns>
        /// List of possible moves or null
        /// </returns>
        public List<Move>? EnumMoveList(PlayerColor playerColor, bool isMoveListNeeded, out AttackPosInfo attackPosInfo)
        {
            List<Move>? retVal;
            PieceType pieceType;
            bool isBlackToMove;

            m_posInfo.PiecesAttacked = 0;
            m_posInfo.PiecesDefending = 0;
            retVal = isMoveListNeeded ? new List<Move>(256) : null;
            isBlackToMove = playerColor == PlayerColor.Black;
            for (int i = 0; i < 64; i++)
            {
                pieceType = m_board[i];
                if (pieceType != PieceType.None && (pieceType & PieceType.Black) != 0 == isBlackToMove)
                {
                    switch (pieceType & PieceType.PieceMask)
                    {
                        case PieceType.Pawn:
                            EnumPawnMove(playerColor, i, retVal);
                            break;
                        case PieceType.Knight:
                            EnumFromArray(playerColor, i, s_caseMoveKnight[i], retVal);
                            break;
                        case PieceType.Bishop:
                            EnumFromArray(playerColor, i, s_caseMoveDiagonal[i], retVal);
                            break;
                        case PieceType.Rook:
                            EnumFromArray(playerColor, i, s_caseMoveLine[i], retVal);
                            break;
                        case PieceType.Queen:
                            EnumFromArray(playerColor, i, s_caseMoveDiagLine[i], retVal);
                            break;
                        case PieceType.King:
                            EnumFromArray(playerColor, i, s_caseMoveKing[i], retVal);
                            break;
                    }
                }
            }
            EnumCastleMove(playerColor, retVal);
            EnumEnPassant(playerColor, retVal);
            attackPosInfo = m_posInfo;
            return retVal;
        }


        /// <summary>
        /// Open the design mode
        /// </summary>
        public void OpenDesignMode()
        {
            IsDesignMode = true;
        }

        /// <summary>
        /// Reset the board to the initial configuration
        /// </summary>
        public void ResetBoard()
        {
            for (int i = 0; i < 64; i++)
            {
                m_board[i] = PieceType.None;
            }
            for (int i = 0; i < 8; i++)
            {
                m_board[8 + i] = PieceType.Pawn | PieceType.White;
                m_board[48 + i] = PieceType.Pawn | PieceType.Black;
            }
            m_board[0] = PieceType.Rook | PieceType.White;
            m_board[7 * 8] = PieceType.Rook | PieceType.Black;
            m_board[7] = PieceType.Rook | PieceType.White;
            m_board[(7 * 8) + 7] = PieceType.Rook | PieceType.Black;
            m_board[1] = PieceType.Knight | PieceType.White;
            m_board[(7 * 8) + 1] = PieceType.Knight | PieceType.Black;
            m_board[6] = PieceType.Knight | PieceType.White;
            m_board[(7 * 8) + 6] = PieceType.Knight | PieceType.Black;
            m_board[2] = PieceType.Bishop | PieceType.White;
            m_board[(7 * 8) + 2] = PieceType.Bishop | PieceType.Black;
            m_board[5] = PieceType.Bishop | PieceType.White;
            m_board[(7 * 8) + 5] = PieceType.Bishop | PieceType.Black;
            m_board[3] = PieceType.King | PieceType.White;
            m_board[(7 * 8) + 3] = PieceType.King | PieceType.Black;
            m_board[4] = PieceType.Queen | PieceType.White;
            m_board[(7 * 8) + 4] = PieceType.Queen | PieceType.Black;
            ResetInitialBoardInfo(PlayerColor.White,
                                  isStdBoard: true,
                                  BoardStateMask.BLCastling | BoardStateMask.BRCastling | BoardStateMask.WLCastling | BoardStateMask.WRCastling,
                                  enPassantPos: 0);
        }


        /// <summary>
        /// Reset initial board info
        /// </summary>
        /// <param name="nextMoveColor"> Next color moving</param>
        /// <param name="isStdBoard">    true if its a standard board, false if coming from FEN or design mode</param>
        /// <param name="boardMask">     Extra bord information</param>
        /// <param name="enPassantPos">  Position for en passant</param>
        private void ResetInitialBoardInfo(PlayerColor nextMoveColor, bool isStdBoard, BoardStateMask boardMask, int enPassantPos)
        {
            PieceType pieceType;
            int enPassantCol;

            Array.Clear(m_pieceTypeCount, 0, m_pieceTypeCount.Length);
            for (int i = 0; i < 64; i++)
            {
                pieceType = m_board[i];
                switch (pieceType)
                {
                    case PieceType.King | PieceType.White:
                        m_whiteKingPos = i;
                        break;
                    case PieceType.King | PieceType.Black:
                        m_blackKingPos = i;
                        break;
                }
                m_pieceTypeCount[(int)pieceType]++;
            }
            if (enPassantPos != 0)
            {
                enPassantCol = enPassantPos >> 3;
                if (enPassantCol is not 2 and not 5)
                {
                    if (enPassantCol == 3)
                    {   // Fixing old saved board which was keeping the en passant position at the position of the pawn instead of behind it
                        enPassantPos -= 8;
                    }
                    else if (enPassantCol == 4)
                    {
                        enPassantPos += 8;
                    }
                    else
                    {
                        enPassantPos = 0;
                    }
                }
            }
            m_possibleEnPassantPos = enPassantPos;
            m_rightBlackRookMoveCount = ((boardMask & BoardStateMask.BRCastling) == BoardStateMask.BRCastling) ? 0 : 1;
            m_leftBlackRookMoveCount = ((boardMask & BoardStateMask.BLCastling) == BoardStateMask.BLCastling) ? 0 : 1;
            m_blackKingMoveCount = 0;
            m_rightWhiteRookMoveCount = ((boardMask & BoardStateMask.WRCastling) == BoardStateMask.WRCastling) ? 0 : 1;
            m_leftWhiteRookMoveCount = ((boardMask & BoardStateMask.WLCastling) == BoardStateMask.WLCastling) ? 0 : 1;
            m_whiteKingMoveCount = 0;
            m_isWhiteCastled = false;
            m_isBlackCastled = false;
            ZobristKey = ZobristKeyUtil.ComputeBoardZobristKey(m_board);
            CurrentPlayer = nextMoveColor;
            IsDesignMode = false;
            IsStdInitialBoard = isStdBoard;
            MoveHistory.Reset(m_board, ComputeBoardExtraInfo(addRepetitionInfo: false));
            MovePosStack.Clear();
            m_pPossibleEnPassantPosStack.Clear();
        }

        /// <summary>
        /// Compute extra information about the board
        /// </summary>
        /// <param name="addRepetitionInfo"> true to add board repetition information</param>
        /// <returns>
        /// Extra information about the board to discriminate between two boards with sames pieces but
        /// different setting.
        /// </returns>
        public BoardStateMask ComputeBoardExtraInfo(bool addRepetitionInfo)
        {
            BoardStateMask retVal;

            retVal = (BoardStateMask)m_possibleEnPassantPos;
            if (m_whiteKingMoveCount == 0)
            {
                if (m_rightWhiteRookMoveCount == 0)
                {
                    retVal |= BoardStateMask.WRCastling;
                }
                if (m_leftWhiteRookMoveCount == 0)
                {
                    retVal |= BoardStateMask.WLCastling;
                }
            }
            if (m_blackKingMoveCount == 0)
            {
                if (m_rightBlackRookMoveCount == 0)
                {
                    retVal |= BoardStateMask.BRCastling;
                }
                if (m_leftBlackRookMoveCount == 0)
                {
                    retVal |= BoardStateMask.BLCastling;
                }
            }
            if (addRepetitionInfo)
            {
                retVal = (BoardStateMask)((MoveHistory.GetCurrentSameBoardCount(ZobristKey) & 7) << 11);
            }
            return retVal;
        }


    }
}
