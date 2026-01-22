namespace Torque.Compiler.AST.Statements;




public interface IDeclarationProcessor
{
    void Process(IDeclaration declaration);

    void ProcessVariableDeclaration(VariableDeclarationStatement declaration);
    void ProcessFunctionDeclaration(FunctionDeclarationStatement declaration);
}
