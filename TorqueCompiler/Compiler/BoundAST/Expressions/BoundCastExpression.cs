using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundCastExpression(CastExpression syntax, BoundExpression value) : BoundExpression(syntax)
{
    public new CastExpression Syntax => (base.Syntax as CastExpression)!;

    public BoundExpression Value { get; set; } = value;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessCast(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessCast(this);
}
