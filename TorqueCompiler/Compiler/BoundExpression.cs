using System.Collections.Generic;


namespace Torque.Compiler;




public interface IBoundExpressionProcessor
{
    void Process(BoundExpression expression);

    void ProcessLiteral(BoundLiteralExpression expression);
    void ProcessBinary(BoundBinaryExpression expression);
    void ProcessGrouping(BoundGroupingExpression expression);
    void ProcessSymbol(BoundSymbolExpression expression);
    void ProcessAssignment(BoundAssignmentExpression expression);
    void ProcessCall(BoundCallExpression expression);
    void ProcessCast(BoundCastExpression expression);
}


public interface IBoundExpressionProcessor<out T>
{
    T Process(BoundExpression expression);

    T ProcessLiteral(BoundLiteralExpression expression);
    T ProcessBinary(BoundBinaryExpression expression);
    T ProcessGrouping(BoundGroupingExpression expression);
    T ProcessSymbol(BoundSymbolExpression expression);
    T ProcessAssignment(BoundAssignmentExpression expression);
    T ProcessCall(BoundCallExpression expression);
    T ProcessCast(BoundCastExpression expression);
}




public abstract class BoundExpression(Expression syntax)
{
    public Expression Syntax { get; } = syntax;
    public virtual Type? Type { get; set; }


    public abstract void Process(IBoundExpressionProcessor processor);
    public abstract T Process<T>(IBoundExpressionProcessor<T> processor);


    public Token Source() => Syntax.Source();
}




public class BoundLiteralExpression(LiteralExpression syntax) : BoundExpression(syntax)
{
    public ulong? Value { get; set; }


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessLiteral(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);
}




public class BoundBinaryExpression(BinaryExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public BoundExpression Left { get; } = left;
    public BoundExpression Right { get; } = right;

    public override Type? Type => Left.Type;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessBinary(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);
}




public class BoundGroupingExpression(GroupingExpression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public BoundExpression Expression { get; } = expression;

    public override Type? Type => Expression.Type;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessGrouping(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);
}




public class BoundSymbolExpression(SymbolExpression syntax, ValueSymbol symbol) : BoundExpression(syntax)
{
    public ValueSymbol Symbol { get; } = symbol;
    public bool GetAddress => (Syntax as SymbolExpression)!.GetAddress;

    public override Type? Type => Symbol.Type;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessSymbol(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);
}




public class BoundAssignmentExpression(AssignmentExpression syntax, BoundSymbolExpression symbol, BoundExpression value)
    : BoundExpression(syntax)
{
    public BoundSymbolExpression Symbol { get; } = symbol;
    public BoundExpression Value { get; } = value;

    public override Type? Type => Symbol.Type;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAssignment(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);
}




public class BoundCallExpression(CallExpression syntax, BoundExpression callee, IEnumerable<BoundExpression> arguments)
    : BoundExpression(syntax)
{
    public BoundExpression Callee { get; } = callee;
    public IEnumerable<BoundExpression> Arguments { get; } = arguments;

    // the Symbol.Type of a callee is its return type
    public override Type? Type => Callee.Type;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessCall(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessCall(this);
}




public class BoundCastExpression(CastExpression syntax, BoundExpression value) : BoundExpression(syntax)
{
    public BoundExpression Value { get; } = value;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessCast(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessCast(this);
}
