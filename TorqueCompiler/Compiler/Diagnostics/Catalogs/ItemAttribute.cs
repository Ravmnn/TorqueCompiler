using System;


namespace Torque.Compiler.Diagnostics.Catalogs;




[AttributeUsage(AttributeTargets.Field)]
internal class ItemAttribute(DiagnosticScope scope, DiagnosticSeverity severity = DiagnosticSeverity.Error) : Attribute
{
    public DiagnosticScope Scope { get; } = scope;
    public DiagnosticSeverity Severity { get; } = severity;
}
