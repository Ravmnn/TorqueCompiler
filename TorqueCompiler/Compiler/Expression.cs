using System.Collections.Generic;


namespace Torque.Compiler;




public abstract class Expression
{
    public abstract void Process(IExpressionProcessor processor);
    public abstract T Process<T>(IExpressionProcessor<T> processor);


    public abstract Token Source();
    public abstract SourceLocation Location();
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
    public override SourceLocation Location()
        => new SourceLocation(Left.Location(), Right.Location());
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
    public override SourceLocation Location()
        => new SourceLocation(Operator.Location, Right.Location());
}








public class LiteralExpression(Token value) : Expression
{
    public Token Value { get; } = value;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLiteral(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);


    public override Token Source() => Value;
    public override SourceLocation Location() => Source().Location;
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




public class GroupingExpression(Token leftParen, Expression expression, Token rightParen) : Expression
{
    public Token LeftParen { get; } = leftParen;
    public Expression Expression { get; } = expression;
    public Token RightParen { get; } = rightParen;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessGrouping(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);


    public override Token Source() => Expression.Source();
    public override SourceLocation Location()
        => new SourceLocation(LeftParen.Location, RightParen.Location);
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




public class SymbolExpression(Token? addressOperator, Token identifier) : Expression
{
    public Token? AddressOperator { get; } = addressOperator;
    public Token Identifier { get; } = identifier;

    public bool GetAddress => AddressOperator is not null;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessSymbol(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);


    public override Token Source() => Identifier;

    public override SourceLocation Location()
        => GetAddress ? new SourceLocation(AddressOperator!.Value.Location, Identifier.Location) : Identifier.Location;
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




public class CallExpression(Expression callee, Token leftParen, IReadOnlyList<Expression> arguments, Token rightParen) : Expression
{
    public Expression Callee { get; } = callee;
    public Token LeftParen { get; } = leftParen;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;
    public Token RightParen { get; } = rightParen;


    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCall(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCall(this);


    public override Token Source() => LeftParen;
    public override SourceLocation Location()
        => new SourceLocation(Callee.Location(), RightParen.Location);
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

    public override SourceLocation Location()
        => new SourceLocation(Expression.Location(), Type.Base.TypeToken.Location);
}




public class ArrayExpression(TypeName elementType, Token keyword, ulong size, IReadOnlyList<Expression> elements, Token rightCurlyBracket) : Expression
{
    public TypeName ElementType { get; } = elementType;
    public Token Keyword { get; } = keyword;
    public ulong Size { get; } = size;
    public IReadOnlyList<Expression> Elements { get; } = elements;
    public Token RightCurlyBracket { get; } = rightCurlyBracket;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessArray(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessArray(this);


    public override Token Source()
        => Keyword;

    public override SourceLocation Location()
        => new SourceLocation(ElementType.Base.TypeToken, RightCurlyBracket);
}




public class IndexingExpression(Expression pointer, Token leftSquareBracket, Expression index, Token rightSquareBracket) : Expression
{
    public Expression Pointer { get; } = pointer;
    public Token LeftSquareBracket { get; } = leftSquareBracket;
    public Expression Index { get; } = index;
    public Token RightSquareBracket { get; } = rightSquareBracket;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessIndexing(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessIndexing(this);


    public override Token Source()
        => LeftSquareBracket;

    public override SourceLocation Location()
        => new SourceLocation(Pointer.Location(), RightSquareBracket);
}




public class DefaultExpression(Token keyword, Token leftParen, TypeName typeName, Token rightParen) : Expression
{
    public Token Keyword { get; } = keyword;
    public Token LeftParen { get; } = leftParen;
    public TypeName TypeName { get; } = typeName;
    public Token RightParen { get; } = rightParen;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessDefault(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessDefault(this);


    public override Token Source()
        => Keyword;

    public override SourceLocation Location()
        => new SourceLocation(Keyword, RightParen);
}
