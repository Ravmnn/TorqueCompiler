namespace Torque.Compiler.AST.Statements;




public interface IDeclarationProcessor
{
    void Process(IDeclaration declaration);

    void ProcessFunctionDeclaration(FunctionDeclarationStatement declaration);
    void ProcessAliasDeclaration(AliasDeclarationStatement declaration);
    void ProcessStructDeclaration(StructDeclarationStatement declaration);
}
