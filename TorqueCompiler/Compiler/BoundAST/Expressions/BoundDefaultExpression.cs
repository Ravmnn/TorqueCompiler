using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundDefaultExpression(DefaultExpression syntax) : BoundExpression(syntax)
{
    public new DefaultExpression Syntax => (base.Syntax as DefaultExpression)!;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessDefault(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessDefault(this);
}
