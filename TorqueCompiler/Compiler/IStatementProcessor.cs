namespace Torque.Compiler;




public interface IStatementProcessor
{
    void Process(Statement statement);

    void ProcessExpression(ExpressionStatement statement);
    void ProcessDeclaration(DeclarationStatement statement);
    void ProcessFunctionDeclaration(FunctionDeclarationStatement statement);
    void ProcessReturn(ReturnStatement statement);
    void ProcessBlock(BlockStatement statement);
}


public interface IStatementProcessor<out T>
{
    T Process(Statement statement);

    T ProcessExpression(ExpressionStatement statement);
    T ProcessDeclaration(DeclarationStatement statement);
    T ProcessFunctionDeclaration(FunctionDeclarationStatement statement);
    T ProcessReturn(ReturnStatement statement);
    T ProcessBlock(BlockStatement statement);
}
