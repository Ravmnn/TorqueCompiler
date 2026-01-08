using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundAssignmentReferenceExpression(Expression syntax, BoundExpression reference) : BoundExpression(syntax)
{
    public BoundExpression Reference { get; set; } = reference;

    public override Type? Type => Reference.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAssignmentReference(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessAssignmentReference(this);
}
