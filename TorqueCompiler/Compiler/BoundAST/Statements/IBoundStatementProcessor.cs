namespace Torque.Compiler.BoundAST.Statements;




public interface IBoundStatementProcessor
{
    void Process(BoundStatement statement);

    void ProcessExpression(BoundExpressionStatement statement);
    void ProcessDeclaration(BoundDeclarationStatement statement);
    void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement);
    void ProcessReturn(BoundReturnStatement statement);
    void ProcessBlock(BoundBlockStatement statement);
    void ProcessIf(BoundIfStatement statement);
}


public interface IBoundStatementProcessor<T>
{
    T Process(BoundStatement statement);

    T ProcessExpression(BoundExpressionStatement statement);
    T ProcessDeclaration(BoundDeclarationStatement statement);
    T ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement);
    T ProcessReturn(BoundReturnStatement statement);
    T ProcessBlock(BoundBlockStatement statement);
    T ProcessIf(BoundIfStatement statement);
}
