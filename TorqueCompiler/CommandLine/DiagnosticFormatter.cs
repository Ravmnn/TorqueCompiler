using System.Linq;
using System.Text;

using Torque.Compiler;
using Torque.Compiler.Diagnostics;


namespace Torque.CommandLine;




public static class DiagnosticFormatter
{
    public static string Format(Diagnostic diagnostic)
    {
        var severitySpan = diagnostic.Severity.ToString().ToLower();
        var scopeSpan = new string(diagnostic.Scope.ToString().Where(char.IsUpper).ToArray());
        var fileName = SourceCode.FileName ?? "interactive";
        var locationSpan = diagnostic.Location is { } location ? $", {fileName}::{location}" : null;

        var message = diagnostic.GetMessage();
        var formattedMessage = string.Format(message, diagnostic.Arguments.ToArray()); // "ToArray()" is needed to avoid overload ambiguity, do not remove

        var codePeek = diagnostic.Location is not null ? GenerateCodePeek(diagnostic.Location.Value) : null;
        codePeek = $"{(codePeek is not null ? "\n" : "")}{codePeek}";

        return $"(T{scopeSpan}{diagnostic.Code:D3} {severitySpan}{locationSpan})\n{formattedMessage}{codePeek}";
    }




    public static string GenerateCodePeek(TokenLocation location)
    {
        var codeLine = SourceCode.GetLine(location.Line);
        var indicator = GenerateCodePeekIndicator(location);

        return $"{location.Line} |  {codeLine}\n{indicator}";
    }


    public static string GenerateCodePeekIndicator(TokenLocation location)
    {
        var indicatorString = new StringBuilder();

        var initialOffsetAmount = location.Line.ToString().Length + 4;
        var initialOffsetString = new string(' ', initialOffsetAmount);

        for (var i = 0; i < location.End; i++)
        {
            if (i < location.Start)
                indicatorString.Append(' ');

            else if (i == location.Start)
                indicatorString.Append('^');

            else
                indicatorString.Append('~');
        }

        return $"{initialOffsetString}{indicatorString}";
    }
}
