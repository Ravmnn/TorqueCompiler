using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




// Why create a declaration logic if the language doesn't even allow the programmer
// to declare something before using it? Because the programmer should be able to
// access an element (type, function) even if it's defined later in the same file,
// so the compiler implicitly declares those elements first before any other processing.

public interface IDeclaration : IModificable
{
    SymbolSyntax Symbol { get; }




    void ProcessDeclaration(IDeclarationProcessor processor);
}
