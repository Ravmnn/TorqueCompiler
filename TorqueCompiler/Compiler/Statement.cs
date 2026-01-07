using System.Collections.Generic;


namespace Torque.Compiler;




public abstract class Statement(Span location)
{
    public Span Location { get; } = location;




    public abstract void Process(IStatementProcessor processor);
    public abstract T Process<T>(IStatementProcessor<T> processor);
}




public class ExpressionStatement(Expression expression) : Statement(expression.Location)
{
    public Expression Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessExpression(this);
}




public class DeclarationStatement(TypeName type, SymbolSyntax name, Expression value) : Statement(name.Location)
{
    public TypeName Type { get; } = type;
    public SymbolSyntax Name { get; } = name;
    public Expression Value { get; } = value;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);
}




public readonly record struct FunctionParameterDeclaration(SymbolSyntax Name, TypeName Type);


public class FunctionDeclarationStatement(TypeName returnType, SymbolSyntax name, IReadOnlyList<FunctionParameterDeclaration> parameters,
    BlockStatement body) : Statement(name.Location)
{
    public TypeName ReturnType { get; } = returnType;
    public SymbolSyntax Name { get; } = name;
    public IReadOnlyList<FunctionParameterDeclaration> Parameters { get; } = parameters;
    public BlockStatement Body { get; } = body;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);
}




public class ReturnStatement(Span location, Expression? expression = null) : Statement(location)
{
    public Expression? Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessReturn(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessReturn(this);
}




public class BlockStatement(IReadOnlyList<Statement> statements, Span location) : Statement(location)
{
    public IReadOnlyList<Statement> Statements { get; } = statements;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessBlock(this);
}




public class IfStatement(Expression condition, Statement thenStatement, Statement? elseStatement, Span location) : Statement(location)
{
    public Expression Condition { get; } = condition;
    public Statement ThenStatement { get; } = thenStatement;
    public Statement? ElseStatement { get; } = elseStatement;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessIf(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessIf(this);
}
