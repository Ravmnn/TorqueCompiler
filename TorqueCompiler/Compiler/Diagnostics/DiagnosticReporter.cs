using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;


namespace Torque.Compiler.Diagnostics;




public abstract class DiagnosticReporter<T> where T : Enum
{
    public List<Diagnostic> Diagnostics { get; } = [];




    public bool HasReports => Diagnostics.Count > 0;




    public virtual Diagnostic Report(T item, string[]? arguments = null, TokenLocation? location = null)
    {
        var diagnostic = Diagnostic.FromCatalog<T>(Convert.ToInt32(item), arguments, location);
        Diagnostics.Add(diagnostic);

        return diagnostic;
    }


    public virtual Diagnostic Report(Diagnostic diagnostic)
    {
        Diagnostics.Add(diagnostic);
        return diagnostic;
    }




    [DoesNotReturn]
    public virtual void ReportAndThrow(T item, string[]? arguments = null, TokenLocation? location = null)
    {
        var diagnostic = Report(item, arguments, location);
        throw new DiagnosticException(diagnostic);
    }


    [DoesNotReturn]
    public virtual void ReportAndThrow(Diagnostic diagnostic)
    {
        Report(diagnostic);
        throw new DiagnosticException(diagnostic);
    }
}
