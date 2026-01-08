using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundAddressExpression(AddressExpression syntax, BoundAddressableExpression expression) : BoundExpression(syntax)
{
    public new AddressExpression Syntax => (base.Syntax as AddressExpression)!;

    public BoundAddressableExpression Expression { get; } = expression;
    public override Type Type => new PointerType(Expression.Type!);




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAddress(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessAddress(this);
}
