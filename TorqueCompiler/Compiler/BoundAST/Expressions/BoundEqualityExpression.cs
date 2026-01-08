using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundEqualityExpression(EqualityExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public new EqualityExpression Syntax => (base.Syntax as EqualityExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;

    public override Type Type => PrimitiveType.Bool;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessEquality(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessEquality(this);
}
