using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




// Why create a declaration logic if the language doesn't even allow the programmer
// to declare something before using it? Because the programmer should be able to
// access an element (type, function) even if it's defined later in the same file,
// so the compiler (binder, type checker) implicitly declares those elements first
// before any other processing. In fact, this class has two purposes:
// 1. Allows the element to be declared first, then defined later. This behavior is
// only supported for file scope declarations, and can only be accessed by the compiler API.
// 2. Provide logic that all declarations support, like modifiers. This behavior is
// supported for all kinds of declarations.
// For function scope only declarations, like a variable, the feature 1 is irrelevant.

public interface IDeclaration : IModificable
{
    SymbolSyntax Symbol { get; }
    bool CanBeInFileScope { get; }
    bool CanBeInFunctionScope { get; }




    void ProcessDeclaration(IDeclarationProcessor processor);
}
