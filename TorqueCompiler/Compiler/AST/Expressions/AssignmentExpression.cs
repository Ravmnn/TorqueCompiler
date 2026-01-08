using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class AssignmentExpression(Expression reference, Expression value, Span location)
    : BinaryLayoutExpression(reference, value, TokenType.Equal, location), IBinaryLayoutExpressionFactory
{
    public Expression Target => Left;
    public Expression Value => Right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessAssignment(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);


    public static BinaryLayoutExpression Create(Expression left, Expression right, TokenType @operator, Span location)
        => new AssignmentExpression(left, right, location);
}
