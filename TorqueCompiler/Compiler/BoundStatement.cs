using System.Collections.Generic;


namespace Torque.Compiler;




public interface IBoundStatementProcessor
{
    void ProcessExpression(BoundExpressionStatement statement);
    void ProcessDeclaration(BoundDeclarationStatement statement);
    void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement);
    void ProcessReturn(BoundReturnStatement statement);
    void ProcessBlock(BoundBlockStatement statement);
}




public abstract class BoundStatement(Statement syntax)
{
    public Statement Syntax { get; } = syntax;


    public abstract void Process(IBoundStatementProcessor processor);
}




public class BoundExpressionStatement(ExpressionStatement syntax, BoundExpression expression) : BoundStatement(syntax)
{
    public BoundExpression Expression { get; } = expression;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessExpression(this);
}




public class BoundDeclarationStatement(DeclarationStatement syntax, ValueSymbol symbol, BoundExpression value) : BoundStatement(syntax)
{
    public ValueSymbol Symbol { get; } = symbol;
    public BoundExpression Value { get; } = value;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessDeclaration(this);
}




public class BoundFunctionDeclarationStatement(FunctionDeclarationStatement syntax, BoundBlockStatement body, FunctionSymbol symbol) : BoundStatement(syntax)
{
    public BoundBlockStatement Body { get; } = body;

    public FunctionSymbol Symbol { get; } = symbol;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);
}




public class BoundReturnStatement(ReturnStatement syntax, BoundExpression? expression) : BoundStatement(syntax)
{
    public BoundExpression? Expression { get; } = expression;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessReturn(this);
}




public class BoundBlockStatement(BlockStatement syntax, IEnumerable<BoundStatement> statements) : BoundStatement(syntax)
{
    public IEnumerable<BoundStatement> Statements { get; } = statements;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessBlock(this);
}



