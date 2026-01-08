using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundLiteralExpression(LiteralExpression syntax) : BoundExpression(syntax)
{
    public new LiteralExpression Syntax => (base.Syntax as LiteralExpression)!;

    public object? Value { get; set; }




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessLiteral(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);
}
