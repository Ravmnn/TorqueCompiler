using System.Collections.Generic;


namespace Torque.Compiler;




public interface IBoundStatementProcessor
{
    void Process(BoundStatement statement);

    void ProcessExpression(BoundExpressionStatement statement);
    void ProcessDeclaration(BoundDeclarationStatement statement);
    void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement);
    void ProcessReturn(BoundReturnStatement statement);
    void ProcessBlock(BoundBlockStatement statement);
}


public interface IBoundStatementProcessor<T>
{
    T Process(BoundStatement statement);

    T ProcessExpression(BoundExpressionStatement statement);
    T ProcessDeclaration(BoundDeclarationStatement statement);
    T ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement);
    T ProcessReturn(BoundReturnStatement statement);
    T ProcessBlock(BoundBlockStatement statement);
}




public abstract class BoundStatement(Statement syntax)
{
    public Statement Syntax { get; } = syntax;


    public abstract void Process(IBoundStatementProcessor processor);
    public abstract T Process<T>(IBoundStatementProcessor<T> processor);


    public Token Source() => Syntax.Source();
}




public class BoundExpressionStatement(ExpressionStatement syntax, BoundExpression expression) : BoundStatement(syntax)
{
    public BoundExpression Expression { get; } = expression;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessExpression(this);
}




public class BoundDeclarationStatement(DeclarationStatement syntax, VariableSymbol symbol, BoundExpression value) : BoundStatement(syntax)
{
    public VariableSymbol Symbol { get; } = symbol;
    public BoundExpression Value { get; } = value;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);
}




public class BoundFunctionDeclarationStatement(FunctionDeclarationStatement syntax, BoundBlockStatement body, FunctionSymbol symbol)
    : BoundStatement(syntax)
{
    public BoundBlockStatement Body { get; } = body;

    public FunctionSymbol Symbol { get; } = symbol;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);
}




public class BoundReturnStatement(ReturnStatement syntax, BoundExpression? expression) : BoundStatement(syntax)
{
    public BoundExpression? Expression { get; } = expression;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessReturn(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessReturn(this);
}




public class BoundBlockStatement(Scope scope, BlockStatement syntax, IEnumerable<BoundStatement> statements) : BoundStatement(syntax)
{
    public Scope Scope { get; } = scope;
    public IEnumerable<BoundStatement> Statements { get; } = statements;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessBlock(this);
}
