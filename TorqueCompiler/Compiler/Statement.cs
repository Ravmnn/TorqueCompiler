using System.Collections.Generic;


namespace Torque.Compiler;




public interface IStatementProcessor
{
    void ProcessExpression(ExpressionStatement statement);
    void ProcessDeclaration(DeclarationStatement statement);
    void ProcessFunctionDeclaration(FunctionDeclarationStatement statement);
    void ProcessReturn(ReturnStatement statement);
    void ProcessBlock(BlockStatement statement);
}




public abstract class Statement
{
    public abstract void Process(IStatementProcessor processor);

    public abstract Token Source();
}




public class ExpressionStatement(Expression expression) : Statement
{
    public Expression Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessExpression(this);


    public override Token Source() => Expression.Source();
}



public class DeclarationStatement(Token name, Token type, Expression value) : Statement
{
    public Token Name { get; } = name;
    public Token Type { get; } = type;
    public Expression Value { get; } = value;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override Token Source() => Type;
}



public readonly record struct FunctionParameterDeclaration(Token Name, Token Type);


public class FunctionDeclarationStatement(Token name, Token returnType, IEnumerable<FunctionParameterDeclaration> parameters,
    BlockStatement body) : Statement
{
    public Token Name { get; } = name;
    public Token ReturnType { get; } = returnType;
    public IEnumerable<FunctionParameterDeclaration> Parameters { get; } = parameters;
    public BlockStatement Body { get; } = body;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override Token Source() => ReturnType;
}



public class ReturnStatement(Token keyword, Expression expression) : Statement
{
    public Token Keyword { get; } = keyword;
    public Expression Expression { get; } = expression;




    public override void Process(IStatementProcessor processor)
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


    public override Token Source() => Start;
}
