using Chess.Domain;

namespace Chess.Pgn.Parser.Tests
{
    [TestClass]
    public class PgnParsingTests
    {
        [TestMethod]
        public void Parser_Should_Parse_Simple_Game_Without_Error()
        {
            var parser = new PgnParser(false);

            var pgnGame = @"[Event ""Veresov-Lines""]
[Site ""?""]
[Date ""2023.01.09""]
[Round ""?""]
[White ""?""]
[Black ""?""]
[Result ""*""]
[EventDate ""????.??.??""]
[ECO ""D01""]
[PlyCount ""20""]

1.d4 Nf6 2.Nc3 d5 3.Bg5 Nbd7 4.f3 c6 5.e4 dxe4 6.fxe4 e5 7.dxe5 Qa5 8.Bxf6 gxf6 
9.e6 fxe6 10.Qg4 Qg5";
            parser.InitFromString(pgnGame);
            var parsingResult = parser.ParseSingle(ignoreMoveListIfFen: false,
                                        out int skipped,
                                        out int truncated,
                                        out PgnGame? parsedGame,
                                        out string? errTxt);

            Assert.IsTrue(parsingResult);
        }
    }
}