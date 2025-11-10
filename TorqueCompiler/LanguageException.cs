using System;
using System.Text;
using Torque.Compiler;


namespace Torque;




public class LanguageException : Exception
{
    public TokenLocation? Location { get; }




    public LanguageException(string message, TokenLocation? location = null) : base(message)
    {
        Location = location;
    }

    public LanguageException(string message, TokenLocation? location, Exception inner) : base(message, inner)
    {
        Location = location;
    }




    public override string ToString()
    {
        var locationString = Location is not null ? $" (at {Location})" : "";

        var line = Location is not null ? Torque.GetSourceLine(Location.Value.Line) : "";
        var lineString = Location is not null ?
            $"\n{Location.Value.Line}. {line}" : "";

        var indicatorString = GetIndicatorString();

        return $"{Message}{locationString}{lineString}{indicatorString}";
    }


    private string GetIndicatorString()
    {
        if (Location is null)
            return string.Empty;

        var indicatorString = new StringBuilder();

        var initialOffsetAmount = Location?.Line.ToString().Length + 2 ?? 0;
        var initialOffsetString = new string(' ', initialOffsetAmount);

        if (Location is not null)
            for (var i = 0; i < Location.Value.End; i++)
            {
                if (i < Location.Value.Start)
                    indicatorString.Append(' ');

                else if (i == Location.Value.Start)
                    indicatorString.Append('^');

                else
                    indicatorString.Append('~');
            }

        return $"\n{initialOffsetString}{indicatorString}";
    }
}
