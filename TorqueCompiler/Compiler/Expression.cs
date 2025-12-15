using System.Collections.Generic;


namespace Torque.Compiler;




public abstract class Expression
{
    public abstract void Process(IExpressionProcessor processor);
    public abstract T Process<T>(IExpressionProcessor<T> processor);


    public abstract Token Source();
}





public interface IBinaryLayoutExpressionFactory
{
    public static abstract BinaryLayoutExpression Create(Expression left, Token @operator, Expression right);
}


public abstract class BinaryLayoutExpression(Expression left, Token @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Token Operator { get; } = @operator;
    public Expression Right { get; } = right;


    public override Token Source() => Operator;
}




public interface IUnaryLayoutExpressionFactory
{
    public static abstract UnaryLayoutExpression Create(Token @operator, Expression right);
}


public abstract class UnaryLayoutExpression(Token @operator, Expression right) : Expression
{
    public Token Operator { get; } = @operator;
    public Expression Right { get; } = right;


    public override Token Source() => Operator;
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




public class BinaryExpression(Expression left, Token @operator, Expression right)
    : BinaryLayoutExpression(left, @operator, right), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessBinary(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);


    public static BinaryLayoutExpression Create(Expression left, Token @operator, Expression right)
        => new BinaryExpression(left, @operator, right);
}




public class UnaryExpression(Token @operator, Expression expression)
    : UnaryLayoutExpression(@operator, expression), IUnaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessUnary(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessUnary(this);


    public static UnaryLayoutExpression Create(Token @operator, Expression right)
        => new UnaryExpression(@operator, right);
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




public class ComparisonExpression(Expression left, Token @operator, Expression right)
    : BinaryLayoutExpression(left, @operator, right), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessComparison(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessComparison(this);


    public static BinaryLayoutExpression Create(Expression left, Token @operator, Expression right)
        => new ComparisonExpression(left, @operator, right);
}




public class EqualityExpression(Expression left, Token @operator, Expression right)
    : BinaryLayoutExpression(left, @operator, right), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessEquality(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessEquality(this);


    public static BinaryLayoutExpression Create(Expression left, Token @operator, Expression right)
        => new EqualityExpression(left, @operator, right);
}




public class LogicExpression(Expression left, Token @operator, Expression right)
    : BinaryLayoutExpression(left, @operator, right), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLogic(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLogic(this);


    public static BinaryLayoutExpression Create(Expression left, Token @operator, Expression right)
        => new LogicExpression(left, @operator, right);
}




public class SymbolExpression(Token identifier, bool getAddress = false) : Expression
{
    public Token Identifier { get; } = identifier;
    public bool GetAddress { get; } = getAddress;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessSymbol(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);


    public override Token Source() => Identifier;
}




public class AssignmentExpression(Expression pointer, Token @operator, Expression value)
    : BinaryLayoutExpression(pointer, @operator, value), IBinaryLayoutExpressionFactory
{
    public Expression Pointer => Left;
    public Expression Value => Right;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessAssignment(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);


    public static BinaryLayoutExpression Create(Expression left, Token @operator, Expression right)
        => new AssignmentExpression(left, @operator, right);
}




public class PointerAccessExpression(Token @operator, Expression pointer)
    : UnaryLayoutExpression(@operator, pointer), IUnaryLayoutExpressionFactory
{
    public Expression Pointer => Right;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessPointerAccess(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessPointerAccess(this);


    public static UnaryLayoutExpression Create(Token @operator, Expression right)
        => new PointerAccessExpression(@operator, right);
}




public class CallExpression(Token leftParen, Expression callee, IReadOnlyList<Expression> arguments) : Expression
{
    public Token LeftParen { get; } = leftParen;
    public Expression Callee { get; } = callee;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCall(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCall(this);


    public override Token Source() => LeftParen;
}





public class CastExpression(Expression expression, Token keyword, TypeName type) : Expression
{
    public Expression Expression { get; } = expression;
    public Token Keyword { get; } = keyword;
    public TypeName Type { get; } = type;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCast(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCast(this);


    public override Token Source() => Keyword;
}
