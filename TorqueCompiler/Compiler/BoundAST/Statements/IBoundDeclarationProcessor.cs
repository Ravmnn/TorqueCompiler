namespace Torque.Compiler.BoundAST.Statements;




public interface IBoundDeclarationProcessor
{
    void Process(IBoundDeclaration declaration);

    void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement declaration);
}
