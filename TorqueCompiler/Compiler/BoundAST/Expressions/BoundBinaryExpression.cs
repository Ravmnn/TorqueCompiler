using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundBinaryExpression(BinaryExpression syntax, BoundExpression left, BoundExpression right)
    : BoundExpression(syntax), IBoundBinaryLayoutExpression
{
    public new BinaryExpression Syntax => (base.Syntax as BinaryExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessBinary(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);
}
