using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace Torque.Compiler.Diagnostics;




public abstract class DiagnosticReporter<T> where T : Enum
{
    public List<Diagnostic> Diagnostics { get; } = [];




    public bool HasReports => Diagnostics.Count > 0;




    public virtual Diagnostic Report(T item, object[]? arguments = null, TokenLocation? location = null)
    {
        var diagnostic = Diagnostic.FromCatalog<T>(Convert.ToInt32(item), arguments, location);
        Diagnostics.Add(diagnostic);

        return diagnostic;
    }


    public virtual Diagnostic ReportToken(T item, Token? token = null)
        => Report(item, token is not null ? [token.Value.Lexeme] : null, token?.Location);


    public virtual Diagnostic Report(Diagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
        return diagnostic;
    }




    [DoesNotReturn]
    public virtual void ReportAndThrow(T item, object[]? arguments = null, TokenLocation? location = null)
    {
        var diagnostic = Report(item, arguments, location);
        throw new DiagnosticException(diagnostic);
    }


    [DoesNotReturn]
    public virtual void ReportTokenAndThrow(T item, Token? token = null)
    {
        var diagnostic = ReportToken(item, token);
        throw new DiagnosticException(diagnostic);
    }


    [DoesNotReturn]
    public virtual void ReportAndThrow(Diagnostic diagnostic)
    {
        Report(diagnostic);
        throw new DiagnosticException(diagnostic);
    }
}
