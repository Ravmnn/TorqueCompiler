using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundAssignmentExpression(AssignmentExpression syntax, BoundAssignmentReferenceExpression reference, BoundExpression value)
    : BoundExpression(syntax)
{
    public new AssignmentExpression Syntax => (base.Syntax as AssignmentExpression)!;

    public BoundAssignmentReferenceExpression Reference { get; set; } = reference;
    public BoundExpression Value { get; set; } = value;

    public override Type? Type => Reference.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAssignment(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);
}
