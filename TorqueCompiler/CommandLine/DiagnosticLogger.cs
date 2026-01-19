using System;
using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.CommandLine;




public class DiagnosticLogger
{
    public bool HasError { get; private set; }




    public void LogDiagnosticsAndInterruptIfAny(IReadOnlyList<Diagnostic> diagnostics)
    {
        LogDiagnosticsIfAny(diagnostics);
        InterruptIfHasError();
    }


    public void LogDiagnosticsIfAny(IReadOnlyList<Diagnostic> diagnostics)
    {
        if (!diagnostics.Any())
            return;

        foreach (var diagnostic in diagnostics)
            LogDiagnostic(diagnostic);
    }


    private void LogDiagnostic(Diagnostic diagnostic)
    {
        Console.WriteLine(DiagnosticFormatter.Format(diagnostic));

        if (diagnostic.Severity == DiagnosticSeverity.Error)
            HasError = true;
    }


    private void InterruptIfHasError()
    {
        if (HasError)
            throw new InterruptCompileException();
    }




    public void LogInternalError(Exception exception)
        => Console.WriteLine($@"Internal Error: {exception}");
}
