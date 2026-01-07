using System.Collections.Generic;


namespace Torque.Compiler;





public abstract class BoundStatement(Statement syntax)
{
    public Statement Syntax { get; } = syntax;
    public Span Location => Syntax.Location;




    public abstract void Process(IBoundStatementProcessor processor);
    public abstract T Process<T>(IBoundStatementProcessor<T> processor);
}




public class BoundExpressionStatement(ExpressionStatement syntax, BoundExpression expression) : BoundStatement(syntax)
{
    public new ExpressionStatement Syntax => (base.Syntax as ExpressionStatement)!;

    public BoundExpression Expression { get; } = expression;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessExpression(this);
}




public class BoundDeclarationStatement(DeclarationStatement syntax, VariableSymbol symbol, BoundExpression value) : BoundStatement(syntax)
{
    public new DeclarationStatement Syntax => (base.Syntax as DeclarationStatement)!;

    public BoundExpression Value { get; set; } = value;

    public VariableSymbol Symbol { get; } = symbol;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);
}




public class BoundFunctionDeclarationStatement(FunctionDeclarationStatement syntax, BoundBlockStatement body, FunctionSymbol symbol)
    : BoundStatement(syntax)
{
    public new FunctionDeclarationStatement Syntax => (base.Syntax as FunctionDeclarationStatement)!;

    public BoundBlockStatement Body { get; } = body;

    public FunctionSymbol Symbol { get; } = symbol;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);
}




public class BoundReturnStatement(ReturnStatement syntax, BoundExpression? expression) : BoundStatement(syntax)
{
    public new ReturnStatement Syntax => (base.Syntax as ReturnStatement)!;

    public BoundExpression? Expression { get; set; } = expression;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessReturn(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessReturn(this);
}




public class BoundBlockStatement(BlockStatement syntax,  IReadOnlyList<BoundStatement> statements, Scope scope) : BoundStatement(syntax)
{
    public new BlockStatement Syntax => (base.Syntax as BlockStatement)!;

    public IReadOnlyList<BoundStatement> Statements { get; } = statements;

    public Scope Scope { get; } = scope;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessBlock(this);
}




public class BoundIfStatement(IfStatement syntax, BoundExpression condition, BoundStatement thenStatement, BoundStatement? elseStatement)
    : BoundStatement(syntax)
{
    public new IfStatement Syntax => (base.Syntax as IfStatement)!;

    public BoundExpression Condition { get; set; } = condition;
    public BoundStatement ThenStatement { get; } = thenStatement;
    public BoundStatement? ElseStatement { get; } = elseStatement;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessIf(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessIf(this);
}
