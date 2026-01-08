using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundAddressableExpression(Expression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public BoundExpression Expression { get; } = expression;

    public override Type? Type => Expression.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAddressable(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessAddressable(this);
}
