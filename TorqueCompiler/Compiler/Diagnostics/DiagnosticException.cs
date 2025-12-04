using System;


namespace Torque.Compiler.Diagnostics;




public class DiagnosticException : Exception
{
    public Diagnostic Diagnostic { get; }




    public DiagnosticException(Diagnostic diagnostic) : base(diagnostic.MessageId)
    {
        Diagnostic = diagnostic;
    }

    public DiagnosticException(Diagnostic diagnostic, Exception inner) : base(diagnostic.MessageId, inner)
    {
        Diagnostic = diagnostic;
    }
}
