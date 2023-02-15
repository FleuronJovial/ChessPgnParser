using Superpower;
using Superpower.Parsers;

namespace Chess.Pgn.Parser
{
    public static class DateTimeTextParser
    {
        private static TextParser<int> IntDigits(int count)
        {
            return Character.Digit
                .Repeat(count)
                .Select(chars => int.Parse(new string(chars)));
        }

        private static TextParser<int> TwoDigits { get; } = IntDigits(2);
        private static TextParser<int> FourDigits { get; } = IntDigits(4);

        private static TextParser<char> Dash { get; } = Character.EqualTo('-');
        private static TextParser<char> Colon { get; } = Character.EqualTo(':');
        private static TextParser<char> TimeSeparator { get; } = Character.In('T', ' ');

        private static TextParser<DateTime> Date { get; } =
            from year in FourDigits
            from _ in Dash
            from month in TwoDigits
            from __ in Dash
            from day in TwoDigits
            select new DateTime(year, month, day);

        private static TextParser<TimeSpan> Time { get; } =
            from hour in TwoDigits
            from _ in Colon
            from minute in TwoDigits
            from second in Colon
                .IgnoreThen(TwoDigits)
                .OptionalOrDefault()
            select new TimeSpan(hour, minute, second);

        private static TextParser<DateTime> DateTime { get; } =
            from date in Date
            from time in TimeSeparator
                .IgnoreThen(Time)
                .OptionalOrDefault()
            select date + time;

        private static TextParser<DateTime> DateTimeOnly { get; } = DateTime.AtEnd();

        public static DateTime Parse(string input)
        {
            return DateTimeOnly.Parse(input);
        }
    }
}