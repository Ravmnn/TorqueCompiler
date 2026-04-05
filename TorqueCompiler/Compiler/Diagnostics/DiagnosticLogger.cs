using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Torque.CommandLine.Exceptions;


namespace Torque.Compiler.Diagnostics;




public static class DiagnosticLogger
{
    public static IDiagnosticFormatter Formatter { get; set; } = new DefaultDiagnosticFormatter();
    public static StreamWriter Stream { get; set; } = new StreamWriter(Console.OpenStandardError()) { AutoFlush = true };




    public static void LogDiagnosticsAndInterruptIfAny(IEnumerable<Diagnostic> diagnostics)
    {
        LogDiagnosticsIfAny(diagnostics);
        InterruptIfAnyDiagnosticIsError(diagnostics);
    }


    private static void InterruptIfAnyDiagnosticIsError(IEnumerable<Diagnostic> diagnostics)
    {
        if (diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error))
            throw new InterruptCompileException();
    }


    public static void LogDiagnosticsIfAny(IEnumerable<Diagnostic> diagnostics)
    {
        if (!diagnostics.Any())
            return;

        foreach (var diagnostic in diagnostics)
            LogDiagnostic(diagnostic);
    }


    private static void LogDiagnostic(Diagnostic diagnostic)
        => Stream.WriteLine(Formatter.Format(diagnostic));




    public static void LogInternalError(Exception exception)
        => Stream.WriteLine($@"Internal Error: {exception}");
}
