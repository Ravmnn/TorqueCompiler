using System.IO;
using System.Linq;
using System.Text;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.Diagnostics;




internal readonly struct DiagnosticHeaderMessage(Diagnostic diagnostic)
{
    public Diagnostic Diagnostic { get; } = diagnostic;

    public string Severity => Diagnostic.Severity.ToString().ToLower();
    public string Scope => new string(Diagnostic.Scope.ToString().Where(char.IsUpper).ToArray());
    public string FileName => Diagnostic.SourceCode.File.Name;
    public string Location => Diagnostic.Location is { } location ? $", {FileName}::{location}" : "unknown";




    public override string ToString()
        => $"T{Scope}{Diagnostic.Code:D3} {Severity}{Location}";
}





public class DefaultDiagnosticFormatter : IDiagnosticFormatter
{
    public string Format(Diagnostic diagnostic)
    {
        var diagnosticHeader = new DiagnosticHeaderMessage(diagnostic);
        var codePeek = GenerateCodePeekOrEmpty(diagnostic);

        return $"({diagnosticHeader})\n{diagnostic.GetFormattedMessage()}{codePeek}";
    }


    private static string GenerateCodePeekOrEmpty(Diagnostic diagnostic)
    {
        var shouldGenerateCodePeek = diagnostic.Location is not null;
        var codePeek = shouldGenerateCodePeek ? GenerateCodePeek(diagnostic.SourceCode.File, diagnostic.Location!.Value) : null;
        codePeek = $"{(shouldGenerateCodePeek ? "\n" : "")}{codePeek}";

        return codePeek;
    }


    public static string GenerateCodePeek(FileInfo file, Span location)
    {
        var contentAsLines = File.ReadAllLines(file.FullName);
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
