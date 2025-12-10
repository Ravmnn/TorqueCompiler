using System.Collections.Generic;


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




public abstract class Statement
{
    public abstract void Process(IStatementProcessor processor);
    public abstract T Process<T>(IStatementProcessor<T> processor);


    public abstract Token Source();
}




public class ExpressionStatement(Expression expression) : Statement
{
    public Expression Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessExpression(this);


    public override Token Source() => Expression.Source();
}




public class DeclarationStatement(Token name, TypeName type, Expression value) : Statement
{
    public Token Name { get; } = name;
    public TypeName Type { get; } = type;
    public Expression Value { get; } = value;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);


    public override Token Source() => Name;
}




public readonly record struct FunctionParameterDeclaration(Token Name, TypeName Type);



// TODO: search for IEnumerable<T> that can be replaced to List<T> or T[]
public class FunctionDeclarationStatement(Token name, TypeName returnType, IEnumerable<FunctionParameterDeclaration> parameters,
    BlockStatement body) : Statement
{
    public Token Name { get; } = name;
    public TypeName ReturnType { get; } = returnType;
    public IEnumerable<FunctionParameterDeclaration> Parameters { get; } = parameters;
    public BlockStatement Body { get; } = body;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);


    public override Token Source() => Name;
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
}




public class BlockStatement(Token start, Token end, IEnumerable<Statement> statements) : Statement
{
    public Token Start { get; } = start;
    public Token End { get; } = end;
    public IEnumerable<Statement> Statements { get; } = statements;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessBlock(this);


    public override Token Source() => Start;
}
