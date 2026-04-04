namespace Torque.Compiler.Diagnostics;




public interface IDiagnosticFormatter
{
    string Format(Diagnostic diagnostic);
}
