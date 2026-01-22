namespace Torque.Compiler.AST.Statements;




public interface IStatementProcessor
{
    void Process(Statement statement);

    void ProcessExpression(ExpressionStatement statement);
    void ProcessVariable(VariableDeclarationStatement statement);
    void ProcessFunction(FunctionDeclarationStatement statement);
    void ProcessReturn(ReturnStatement statement);
    void ProcessBlock(BlockStatement statement);
    void ProcessIf(IfStatement statement);
    void ProcessWhile(WhileStatement statement);
    void ProcessContinue(ContinueStatement statement);
    void ProcessBreak(BreakStatement statement);
}


public interface IStatementProcessor<out T>
{
    T Process(Statement statement);

    T ProcessExpression(ExpressionStatement statement);
    T ProcessVariable(VariableDeclarationStatement statement);
    T ProcessFunction(FunctionDeclarationStatement statement);
    T ProcessReturn(ReturnStatement statement);
    T ProcessBlock(BlockStatement statement);
    T ProcessIf(IfStatement statement);
    T ProcessWhile(WhileStatement statement);
    T ProcessContinue(ContinueStatement statement);
    T ProcessBreak(BreakStatement statement);
}
