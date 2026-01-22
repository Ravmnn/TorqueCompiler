namespace Torque.Compiler.BoundAST.Statements;




public interface IBoundStatementProcessor
{
    void Process(BoundStatement statement);

    void ProcessExpression(BoundExpressionStatement statement);
    void ProcessVariable(BoundVariableDeclarationStatement statement);
    void ProcessFunction(BoundFunctionDeclarationStatement statement);
    void ProcessReturn(BoundReturnStatement statement);
    void ProcessBlock(BoundBlockStatement statement);
    void ProcessIf(BoundIfStatement statement);
    void ProcessWhile(BoundWhileStatement statement);
    void ProcessContinue(BoundContinueStatement statement);
    void ProcessBreak(BoundBreakStatement statement);
}


public interface IBoundStatementProcessor<T>
{
    T Process(BoundStatement statement);

    T ProcessExpression(BoundExpressionStatement statement);
    T ProcessDeclaration(BoundVariableDeclarationStatement statement);
    T ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement);
    T ProcessReturn(BoundReturnStatement statement);
    T ProcessBlock(BoundBlockStatement statement);
    T ProcessIf(BoundIfStatement statement);
    T ProcessWhile(BoundWhileStatement statement);
    T ProcessContinue(BoundContinueStatement statement);
    T ProcessBreak(BoundBreakStatement statement);
}
