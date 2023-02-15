using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chess.Pgn.Parser.Enums
{
    /// <summary>
    /// Parsing Phase
    /// </summary>
    public enum ParsingPhase
    {
        /// <summary>No phase set yet</summary>
        None = 0,
        /// <summary>Openning a file</summary>
        OpeningFile = 1,
        /// <summary>Reading the file content into memory</summary>
        ReadingFile = 2,
        /// <summary>Raw parsing the PGN file</summary>
        RawParsing = 3,
        /// <summary>Creating the book</summary>
        CreatingBook = 10,
        /// <summary>Processing is finished</summary>
        Finished = 255
    }
}
