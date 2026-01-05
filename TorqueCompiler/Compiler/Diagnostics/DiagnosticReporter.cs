using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace Torque.Compiler.Diagnostics;




public abstract class DiagnosticReporter<T> where T : Enum
{
    public List<Diagnostic> Diagnostics { get; } = [];


    public bool HasReports => Diagnostics.Count > 0;




    public virtual Diagnostic Report(T item, IReadOnlyList<object>? arguments = null, Span? location = null)
    {
        var diagnostic = Diagnostic.FromCatalog<T>(Convert.ToInt32(item), arguments, location);
        Diagnostics.Add(diagnostic);

        return diagnostic;
    }


    public virtual Diagnostic ReportSymbol(T item, SymbolSyntax? symbol = null)
        => Report(item, symbol is not null ? [symbol.Value.Name] : null, symbol?.Location);


    public virtual Diagnostic Report(Diagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
        return diagnostic;
    }




    [DoesNotReturn]
    public virtual void ReportAndThrow(T item, IReadOnlyList<object>? arguments = null, Span? location = null)
    {
        var diagnostic = Report(item, arguments, location);
        throw new DiagnosticException(diagnostic);
    }


    [DoesNotReturn]
    public virtual void ReportTokenAndThrow(T item, SymbolSyntax? symbol = null)
    {
        var diagnostic = ReportSymbol(item, symbol);
        throw new DiagnosticException(diagnostic);
    }


    [DoesNotReturn]
    public virtual void ReportAndThrow(Diagnostic diagnostic)
    {
        Report(diagnostic);
        throw new DiagnosticException(diagnostic);
    }
}
