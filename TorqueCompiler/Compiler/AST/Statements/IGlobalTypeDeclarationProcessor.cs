namespace Torque.Compiler.AST.Statements;




public interface IGlobalTypeDeclarationProcessor
{
    void Process(GlobalTypeDeclarationStatement declaration);

    void ProcessAlias(AliasDeclarationStatement declaration);
    void ProcessStruct(StructDeclarationStatement declaration);
}


public interface IGlobalTypeDeclarationProcessor<out T>
{
    T Process(GlobalTypeDeclarationStatement declaration);

    T ProcessAlias(AliasDeclarationStatement declaration);
    T ProcessStruct(StructDeclarationStatement declaration);
}
