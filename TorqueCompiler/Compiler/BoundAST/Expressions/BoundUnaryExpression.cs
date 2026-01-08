using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundUnaryExpression(UnaryExpression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public new UnaryExpression Syntax => (base.Syntax as UnaryExpression)!;

    public BoundExpression Expression { get; set; } = expression;

    public override Type? Type => Expression.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessUnary(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessUnary(this);
}
