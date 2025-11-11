using System.Collections.Generic;

namespace Torque.Compiler;




public interface IExpressionProcessor
{
    void ProcessLiteral(LiteralExpression expression);
    void ProcessBinary(BinaryExpression expression);
    void ProcessGrouping(GroupingExpression expression);
    void ProcessIdentifier(IdentifierExpression expression);
    void ProcessCall(CallExpression expression);
    void ProcessCast(CastExpression expression);
}




public abstract class Expression
{
    public abstract void Process(IExpressionProcessor processor);


    public abstract Token Source();
}




public class LiteralExpression(Token value) : Expression
{
    public Token Value { get; } = value;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLiteral(this);


    public override Token Source() => Value;
}




public class BinaryExpression(Expression left, Token @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Expression Right { get; } = right;
    public Token Operator { get; } = @operator;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessBinary(this);


    public override Token Source() => Operator;
}




public class GroupingExpression(Expression expression) : Expression
{
    public Expression Expression { get; } = expression;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessGrouping(this);


    public override Token Source() => Expression.Source();
}




public class IdentifierExpression(Token identifier) : Expression
{
    public Token Identifier { get; } = identifier;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessIdentifier(this);


    public override Token Source() => Identifier;
}




public class CallExpression(Token leftParen, Expression callee, IEnumerable<Expression> arguments) : Expression
{
    public Token LeftParen { get; } = leftParen;
    public Expression Callee { get; } = callee;
    public IEnumerable<Expression> Arguments { get; } = arguments;




    public override void Process(IExpressionProcessor processor)
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


    public override Token Source() => Keyword;
};
