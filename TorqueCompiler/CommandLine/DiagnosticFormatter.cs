using System.Text;

using Torque.Compiler.Tokens;
using Torque.Compiler.Diagnostics;


namespace Torque.CommandLine;




public static class DiagnosticFormatter
{
    public static string Format(Diagnostic diagnostic)
    {
        var diagnosticHeader = new DiagnosticHeaderMessage(diagnostic);

        var codePeek = diagnostic.Location is not null ? GenerateCodePeek(diagnostic.Location.Value) : null;
        codePeek = $"{(codePeek is not null ? "\n" : "")}{codePeek}";

        return $"({diagnosticHeader})\n{diagnostic.GetFormattedMessage()}{codePeek}";
    }




    public static string GenerateCodePeek(Span location)
    {
        var codeLine = SourceCode.GetLine(location.Line);
        var indicator = GenerateCodePeekIndicator(location);

        return $"{location.Line} |  {codeLine}\n{indicator}";
    }


    public static string GenerateCodePeekIndicator(Span location)
    {
        const int ExtraOffset = 4;

        var indicatorString = new StringBuilder();

        var initialOffsetAmount = location.Line.ToString().Length + ExtraOffset;
        var initialOffsetString = new string(' ', initialOffsetAmount);

        for (var i = 0; i < location.End; i++)
            GetIndicatorCharacterFromIndex(indicatorString, location, i);

        return $"{initialOffsetString}{indicatorString}";
    }


    private static void GetIndicatorCharacterFromIndex(StringBuilder indicatorString, Span location, int i)
    {
        if (i < location.Start)
            indicatorString.Append(' ');

        else if (i == location.Start)
            indicatorString.Append('^');

        else
            indicatorString.Append('~');
    }
}
