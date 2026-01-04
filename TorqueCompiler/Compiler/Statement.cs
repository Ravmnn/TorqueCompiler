using System.Collections.Generic;


namespace Torque.Compiler;




public abstract class Statement
{
    public abstract void Process(IStatementProcessor processor);
    public abstract T Process<T>(IStatementProcessor<T> processor);


    public abstract Token Source();
    public abstract SourceLocation Location();
}




public class ExpressionStatement(Expression expression) : Statement
{
    public Expression Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessExpression(this);


    public override Token Source() => Expression.Source();
    public override SourceLocation Location() => Expression.Location();
}




public class DeclarationStatement(TypeName type, Token name, Expression value) : Statement
{
    public TypeName Type { get; } = type;
    public Token Name { get; } = name;
    public Expression Value { get; } = value;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);


    public override Token Source() => Name;
    public override SourceLocation Location()
        => new SourceLocation(Type.Base.TypeToken.Location, Value.Location());
}




public readonly record struct FunctionParameterDeclaration(Token Name, TypeName Type);


public class FunctionDeclarationStatement(TypeName returnType, Token name, IReadOnlyList<FunctionParameterDeclaration> parameters,
    BlockStatement body) : Statement
{
    public TypeName ReturnType { get; } = returnType;
    public Token Name { get; } = name;
    public IReadOnlyList<FunctionParameterDeclaration> Parameters { get; } = parameters;
    public BlockStatement Body { get; } = body;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);


    public override Token Source() => Name;

    public override SourceLocation Location()
        => new SourceLocation(ReturnType.Base.TypeToken.Location, Name.Location);
}




public class ReturnStatement(Token keyword, Expression? expression = null) : Statement
{
    public Token Keyword { get; } = keyword;
    public Expression? Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessReturn(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessReturn(this);


    public override Token Source() => Keyword;

    public override SourceLocation Location()
        => Expression is not null ? new SourceLocation(Keyword.Location, Expression.Location()) : Keyword.Location;
}




public class BlockStatement(Token start, Token end, IReadOnlyList<Statement> statements) : Statement
{
    public Token Start { get; } = start;
    public Token End { get; } = end;
    public IReadOnlyList<Statement> Statements { get; } = statements;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessBlock(this);


    public override Token Source() => Start;
    public override SourceLocation Location() => Start.Location;
}
