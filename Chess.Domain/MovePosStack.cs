using Chess.Domain.Enums;
using System.Globalization;
using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Serialization;

namespace Chess.Domain
{
    /// <summary>
    /// Maintains the list of moves which has been done on a board. The undo moves are kept up to when a new move is done.
    /// </summary>
    public class MovePosStack : IXmlSerializable
    {

        /// <summary>
        /// Class constructor
        /// </summary>
        public MovePosStack()
        {
            List = new List<MoveExt>(512);
            PositionInList = -1;
        }

        /// <summary>
        /// Class constructor (copy constructor)
        /// </summary>
        private MovePosStack(MovePosStack movePosList)
        {
            List = new List<MoveExt>(movePosList.List);
            PositionInList = movePosList.PositionInList;
        }

        /// <summary>
        /// Clone the stack
        /// </summary>
        /// <returns>
        /// Move list
        /// </returns>
        public MovePosStack Clone()
        {
            return new(this);
        }

        /// <summary>
        /// Save to the specified binary writer
        /// </summary>
        /// <param name="writer"> Binary writer</param>
        public void SaveToWriter(System.IO.BinaryWriter writer)
        {
            writer.Write(List.Count);
            writer.Write(PositionInList);
            foreach (MoveExt move in List)
            {
                writer.Write((byte)move.Move.OriginalPiece);
                writer.Write(move.Move.StartPos);
                writer.Write(move.Move.EndPos);
                writer.Write((byte)move.Move.Type);
            }
        }

        /// <summary>
        /// Load from reader
        /// </summary>
        /// <param name="reader"> Binary Reader</param>
        public void LoadFromReader(System.IO.BinaryReader reader)
        {
            int moveCount;
            Move move;

            List.Clear();
            moveCount = reader.ReadInt32();
            PositionInList = reader.ReadInt32();
            for (int i = 0; i < moveCount; i++)
            {
                move.OriginalPiece = (PieceType)reader.ReadByte();
                move.StartPos = reader.ReadByte();
                move.EndPos = reader.ReadByte();
                move.Type = (MoveType)reader.ReadByte();
                List.Add(new MoveExt(move));
            }
        }

        /// <summary>
        /// Returns the XML schema if any
        /// </summary>
        /// <returns>
        /// null
        /// </returns>
        public System.Xml.Schema.XmlSchema? GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Deserialize from XML
        /// </summary>
        /// <param name="reader"> XML reader</param>
        public void ReadXml(XmlReader reader)
        {
            Move move;
            bool isEmpty;

            List.Clear();
            if (reader.MoveToContent() != XmlNodeType.Element || reader.LocalName != "MoveList")
            {
                throw new SerializationException("Unknown format");
            }
            else
            {
                isEmpty = reader.IsEmptyElement;
                PositionInList = int.Parse(reader.GetAttribute("PositionInList") ?? "-1");
                if (isEmpty)
                {
                    _ = reader.Read();
                }
                else
                {
                    if (reader.ReadToDescendant("Move"))
                    {
                        while (reader.IsStartElement())
                        {
                            move = new Move
                            {
                                OriginalPiece = (PieceType)Enum.Parse(typeof(SerPieceType), reader.GetAttribute("OriginalPiece") ?? "0"),
                                StartPos = (byte)int.Parse(reader.GetAttribute("StartingPosition") ?? "0", CultureInfo.InvariantCulture),
                                EndPos = (byte)int.Parse(reader.GetAttribute("EndingPosition") ?? "0", CultureInfo.InvariantCulture),
                                Type = (MoveType)Enum.Parse(typeof(MoveType), reader.GetAttribute("MoveType") ?? "0")
                            };
                            List.Add(new MoveExt(move));
                            reader.ReadStartElement("Move");
                        }
                    }
                    reader.ReadEndElement();
                }
            }
        }

        /// <summary>
        /// Serialize the move list to an XML writer
        /// </summary>
        /// <param name="writer">   XML writer</param>
        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("MoveList");
            writer.WriteAttributeString("PositionInList", PositionInList.ToString());
            foreach (MoveExt move in List)
            {
                writer.WriteStartElement("Move");
                writer.WriteAttributeString("OriginalPiece", ((SerPieceType)move.Move.OriginalPiece).ToString());
                writer.WriteAttributeString("StartingPosition", ((int)move.Move.StartPos).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("EndingPosition", ((int)move.Move.EndPos).ToString(CultureInfo.InvariantCulture));
                writer.WriteAttributeString("MoveType", move.Move.Type.ToString());
                writer.WriteEndElement();
            }
            writer.WriteEndElement();
        }

        /// <summary>
        /// Count
        /// </summary>
        public int Count => List.Count;

        /// <summary>
        /// Indexer
        /// </summary>
        public MoveExt this[int index] => List[index];

        /// <summary>
        /// Get the list of moves
        /// </summary>
        public List<MoveExt> List { get; }

        /// <summary>
        /// Add a move to the stack. All redo move are discarded
        /// </summary>
        /// <param name="move"> New move</param>
        public void AddMove(MoveExt move)
        {
            int count;
            int pos;

            count = Count;
            pos = PositionInList + 1;
            while (count != pos)
            {
                List.RemoveAt(--count);
            }
            List.Add(move);
            PositionInList = pos;
        }

        /// <summary>
        /// Current move (last done move)
        /// </summary>
        public MoveExt CurrentMove => this[PositionInList];

        /// <summary>
        /// Next move in the redo list
        /// </summary>
        public MoveExt NextMove => this[PositionInList + 1];

        /// <summary>
        /// Move to next move
        /// </summary>
        public void MoveToNext()
        {
            int maxPos;

            maxPos = Count - 1;
            if (PositionInList < maxPos)
            {
                PositionInList++;
            }
        }

        /// <summary>
        /// Move to previous move
        /// </summary>
        public void MoveToPrevious()
        {
            if (PositionInList > -1)
            {
                PositionInList--;
            }
        }

        /// <summary>
        /// Current move index
        /// </summary>
        public int PositionInList { get; private set; }

        /// <summary>
        /// Removes all move in the list
        /// </summary>
        public void Clear()
        {
            List.Clear();
            PositionInList = -1;
        }
    } // Class MovePosStack
} // Namespace
