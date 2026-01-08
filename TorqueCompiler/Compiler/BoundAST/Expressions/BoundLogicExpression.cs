using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundLogicExpression(LogicExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public new LogicExpression Syntax => (base.Syntax as LogicExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;

    public override Type Type => PrimitiveType.Bool;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessLogic(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessLogic(this);
}
