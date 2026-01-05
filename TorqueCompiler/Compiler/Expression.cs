using System.Collections.Generic;


namespace Torque.Compiler;


// TODO: urgent, use SourceLocation instead of Token to avoid coupling and improve architecture quality:
// each expression must have a single optional "SourceLocation" (maybe rename to "Span")


public abstract class Expression(Span location)
{
    public Span Location { get; } = location;




    public abstract void Process(IExpressionProcessor processor);
    public abstract T Process<T>(IExpressionProcessor<T> processor);
}





public interface IBinaryLayoutExpressionFactory
{
    public static abstract BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location);
}


public abstract class BinaryLayoutExpression(Expression left, Expression right, TokenType @operator, Span location)
    : Expression(location)
{
    public Expression Left { get; } = left;
    public Expression Right { get; } = right;
    public TokenType Operator { get; } = @operator;
}




public interface IUnaryLayoutExpressionFactory
{
    public static abstract UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location);
}


public abstract class UnaryLayoutExpression(Expression right, TokenType @operator, Span location) : Expression(location)
{
    public Expression Right { get; } = right;
    public TokenType Operator { get; } = @operator;
}








public class LiteralExpression(object value, Span location) : Expression(location)
{
    public object Value { get; } = value;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLiteral(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);
}




public class BinaryExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessBinary(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);




    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new BinaryExpression(left, right, @operator, location);
}




public class UnaryExpression(Expression right, TokenType @operator, Span location)
    : UnaryLayoutExpression(right, @operator, location), IUnaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessUnary(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessUnary(this);


    public static UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location)
        => new UnaryExpression(right, @operator, location);
}




public class GroupingExpression(Expression expression, Span location) : Expression(location)
{
    public Expression Expression { get; } = expression;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessGrouping(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);
}




public class ComparisonExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessComparison(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessComparison(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new ComparisonExpression(left, right, @operator, location);
}




public class EqualityExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessEquality(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessEquality(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new EqualityExpression(left, right, @operator, location);
}




public class LogicExpression(Expression left, Expression right, TokenType @operator, Span location)
    : BinaryLayoutExpression(left, right, @operator, location), IBinaryLayoutExpressionFactory
{
    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLogic(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLogic(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new LogicExpression(left, right, @operator, location);
}




public class SymbolExpression(SymbolSyntax symbol) : Expression(symbol.Location)
{
    public SymbolSyntax Symbol { get; } = symbol;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessSymbol(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);
}




public class AddressExpression(Expression expression, Span location)
    : UnaryLayoutExpression(expression, TokenType.Ampersand, location), IUnaryLayoutExpressionFactory
{
    public Expression Expression => Right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessAddress(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessAddress(this);


    public static UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location)
        => new AddressExpression(right, location);
}




public class AssignmentExpression(Expression pointer, Expression value, Span location)
    : BinaryLayoutExpression(pointer, value, TokenType.Equal, location), IBinaryLayoutExpressionFactory
{
    public Expression Target => Left;
    public Expression Value => Right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessAssignment(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new AssignmentExpression(left, right, location);
}




public class PointerAccessExpression(Expression pointer, Span location)
    : UnaryLayoutExpression(pointer, TokenType.Star, location), IUnaryLayoutExpressionFactory
{
    public Expression Pointer => Right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessPointerAccess(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessPointerAccess(this);


    public static UnaryLayoutExpression Create(Expression right, TokenType @operator, Span location)
        => new PointerAccessExpression(right, location);
}




public class CallExpression(Expression callee, IReadOnlyList<Expression> arguments, Span location) : Expression(location)
{
    public Expression Callee { get; } = callee;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCall(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCall(this);
}





public class CastExpression(Expression expression, TypeName type, Span location) : Expression(location)
{
    public Expression Expression { get; } = expression;
    public TypeName Type { get; } = type;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCast(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCast(this);
}




public class ArrayExpression(TypeName elementType, ulong size, IReadOnlyList<Expression>? elements, Span location) : Expression(location)
{
    public TypeName ElementType { get; } = elementType;
    public ulong Size { get; } = size;
    public IReadOnlyList<Expression>? Elements { get; } = elements;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessArray(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessArray(this);
}




public class IndexingExpression(Expression pointer, Expression index, Span location) : Expression(location)
{
    public Expression Pointer { get; } = pointer;
    public Expression Index { get; } = index;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessIndexing(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessIndexing(this);
}




public class DefaultExpression(TypeName typeName, Span location) : Expression(location)
{
    public TypeName TypeName { get; } = typeName;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessDefault(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessDefault(this);
}
