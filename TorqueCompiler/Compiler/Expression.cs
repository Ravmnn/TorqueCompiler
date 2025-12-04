using System.Collections.Generic;


namespace Torque.Compiler;




// TODO: add another variant IExpressionProcessor<T> and implement in the classes that uses it. do this for Statement too
public interface IExpressionProcessor
{
    void ProcessLiteral(LiteralExpression expression);
    void ProcessBinary(BinaryExpression expression);
    void ProcessGrouping(GroupingExpression expression);
    void ProcessIdentifier(IdentifierExpression expression);
    void ProcessAssignment(AssignmentExpression expression);
    void ProcessCall(CallExpression expression);
    void ProcessCast(CastExpression expression);
}


public interface IExpressionProcessor<out T>
{
    T ProcessLiteral(LiteralExpression expression);
    T ProcessBinary(BinaryExpression expression);
    T ProcessGrouping(GroupingExpression expression);
    T ProcessIdentifier(IdentifierExpression expression);
    T ProcessAssignment(AssignmentExpression expression);
    T ProcessCall(CallExpression expression);
    T ProcessCast(CastExpression expression);
}




public abstract class Expression
{
    public abstract void Process(IExpressionProcessor processor);
    public abstract T Process<T>(IExpressionProcessor<T> processor);


    public abstract Token Source();
}




public class LiteralExpression(Token value) : Expression
{
    public Token Value { get; } = value;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLiteral(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);


    public override Token Source() => Value;
}




public class BinaryExpression(Expression left, Token @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Token Operator { get; } = @operator;
    public Expression Right { get; } = right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessBinary(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);


    public override Token Source() => Operator;
}




public class GroupingExpression(Expression expression) : Expression
{
    public Expression Expression { get; } = expression;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessGrouping(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);


    public override Token Source() => Expression.Source();
}




public class IdentifierExpression(Token identifier, bool getAddress = false) : Expression
{
    public Token Identifier { get; } = identifier;
    public bool GetAddress { get; } = getAddress;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessIdentifier(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessIdentifier(this);


    public override Token Source() => Identifier;
}




public class AssignmentExpression(IdentifierExpression identifier, Token @operator, Expression value) : Expression
{
    public IdentifierExpression Identifier { get; } = identifier;
    public Token Operator { get; } = @operator;
    public Expression Value { get; } = value;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessAssignment(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);


    public override Token Source() => Operator;
}




public class CallExpression(Token leftParen, Expression callee, IEnumerable<Expression> arguments) : Expression
{
    public Token LeftParen { get; } = leftParen;
    public Expression Callee { get; } = callee;
    public IEnumerable<Expression> Arguments { get; } = arguments;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCall(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCall(this);


    public override Token Source() => LeftParen;
}





public class CastExpression(Token keyword, Expression expression, Token type) : Expression
{
    public Token Keyword { get; } = keyword;
    public Expression Expression { get; } = expression;
    public Token Type { get; } = type;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCast(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCast(this);


    public override Token Source() => Keyword;
}
