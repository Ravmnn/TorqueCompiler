using System.IO;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.CommandLine;




public readonly struct DiagnosticHeaderMessage(Diagnostic diagnostic)
{
    public Diagnostic Diagnostic { get; } = diagnostic;

    public string Severity => Diagnostic.Severity.ToString().ToLower();
    public string Scope => new string(Diagnostic.Scope.ToString().Where(char.IsUpper).ToArray());
    public string FileName => SourceCode.FilePath is not null ? new FileInfo(SourceCode.FilePath).Name : "interactive";
    public string Location => Diagnostic.Location is { } location ? $", {FileName}::{location}" : "unknown";




    public override string ToString()
        => $"T{Scope}{Diagnostic.Code:D3} {Severity}{Location}";
}
