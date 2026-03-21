using System.IO;
using System.Text;

using Torque.Compiler.Tokens;
using Torque.Compiler.Diagnostics;


namespace Torque.CommandLine;




public static class DiagnosticFormatter
{
    public static string Format(Diagnostic diagnostic)
    {
        var diagnosticHeader = new DiagnosticHeaderMessage(diagnostic);
        var codePeek = GenerateCodePeekOrEmpty(diagnostic);

        return $"({diagnosticHeader})\n{diagnostic.GetFormattedMessage()}{codePeek}";
    }


    private static string GenerateCodePeekOrEmpty(Diagnostic diagnostic)
    {
        var shouldGenerateCodePeek = diagnostic.Location is not null;
        var codePeek = shouldGenerateCodePeek ? GenerateCodePeek(diagnostic.File, diagnostic.Location!.Value) : null;
        codePeek = $"{(shouldGenerateCodePeek ? "\n" : "")}{codePeek}";

        return codePeek;
    }


    public static string GenerateCodePeek(string file, Span location)
    {
        var contentAsLines = File.ReadAllLines(file);
        var codeLine = contentAsLines[location.Line - 1];
        var indicator = GenerateCodePeekIndicator(location);

        return $"{location.Line} |  {codeLine}\n{indicator}";
    }


    public static string GenerateCodePeekIndicator(Span location)
    {
        const int ExtraMargin = 4;

        var indicatorString = new StringBuilder();

        var marginAmount = location.Line.ToString().Length + ExtraMargin;
        var marginString = new string(' ', marginAmount);

        for (var i = 0; i < location.End; i++)
            GetIndicatorCharacterFromIndex(indicatorString, location, i);

        return $"{marginString}{indicatorString}";
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
