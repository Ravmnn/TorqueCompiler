using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundComparisonExpression(ComparisonExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public new ComparisonExpression Syntax => (base.Syntax as ComparisonExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;

    public override Type Type => PrimitiveType.Bool;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessComparison(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessComparison(this);
}
