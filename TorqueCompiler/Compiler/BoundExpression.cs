using System.Collections.Generic;


namespace Torque.Compiler;




public interface IBoundExpressionProcessor
{
    void ProcessLiteral(BoundLiteralExpression expression);
    void ProcessBinary(BoundBinaryExpression expression);
    void ProcessGrouping(BoundGroupingExpression expression);
    void ProcessIdentifier(BoundIdentifierExpression expression);
    void ProcessAssignment(BoundAssignmentExpression expression);
    void ProcessCall(BoundCallExpression expression);
    void ProcessCast(BoundCastExpression expression);
}




public abstract class BoundExpression(Expression syntax)
{
    public Expression Syntax { get; } = syntax;
    public PrimitiveType? Type { get; set; }


    public abstract void Process(IBoundExpressionProcessor processor);
}




public class BoundLiteralExpression(LiteralExpression syntax) : BoundExpression(syntax)
{
    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessLiteral(this);
}




public class BoundBinaryExpression(BinaryExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public BoundExpression Left { get; } = left;
    public BoundExpression Right { get; } = right;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessBinary(this);
}




public class BoundGroupingExpression(GroupingExpression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public BoundExpression Expression { get; } = expression;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessGrouping(this);
}




public class BoundIdentifierExpression(IdentifierExpression syntax, ValueSymbol value) : BoundExpression(syntax)
{
    public ValueSymbol Value { get; } = value;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessIdentifier(this);
}




public class BoundAssignmentExpression(AssignmentExpression syntax, BoundIdentifierExpression identifier, BoundExpression value)
    : BoundExpression(syntax)
{
    public BoundIdentifierExpression Identifier { get; } = identifier;
    public BoundExpression Value { get; } = value;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAssignment(this);
}




public class BoundCallExpression(CallExpression syntax, BoundExpression callee, IEnumerable<BoundExpression> arguments)
    : BoundExpression(syntax)
{
    public BoundExpression Callee { get; } = callee;
    public IEnumerable<BoundExpression> Arguments { get; } = arguments;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessCall(this);
}




public class BoundCastExpression(CastExpression syntax, BoundExpression value) : BoundExpression(syntax)
{
    public BoundExpression Value { get; } = value;


    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessCast(this);
}



