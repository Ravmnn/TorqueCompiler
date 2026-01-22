using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public interface IDeclaration : IModificable
{
    SymbolSyntax Symbol { get; }




    void ProcessDeclaration(IDeclarationProcessor processor);
}
