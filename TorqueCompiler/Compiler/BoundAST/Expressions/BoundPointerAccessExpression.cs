using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundPointerAccessExpression(PointerAccessExpression syntax, BoundExpression pointer) : BoundExpression(syntax)
{
    public new PointerAccessExpression Syntax => (base.Syntax as PointerAccessExpression)!;

    public BoundExpression Pointer { get; set; } = pointer;

    public override Type? Type => (Pointer.Type as PointerType)?.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessPointerAccess(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessPointerAccess(this);
}
