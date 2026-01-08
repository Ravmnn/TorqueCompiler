using Torque.Compiler.Types;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundImplicitCastExpression(BoundExpression value, Type type) : BoundExpression(value.Syntax)
{
    public BoundExpression Value { get; } = value;
    public override Type? Type { get; set; } = type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessImplicitCast(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessImplicitCast(this);
}
