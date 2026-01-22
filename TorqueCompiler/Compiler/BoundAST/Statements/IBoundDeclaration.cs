using Torque.Compiler.Symbols;


namespace Torque.Compiler.BoundAST.Statements;




public interface IBoundDeclaration
{
    Symbol Symbol { get; }




    void ProcessDeclaration(IBoundDeclarationProcessor processor);
}
