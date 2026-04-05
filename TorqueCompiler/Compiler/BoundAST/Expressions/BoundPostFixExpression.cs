using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundPostFixExpression(Expression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public new PostFixExpression Syntax => (base.Syntax as PostFixExpression)!;

    public BoundExpression Expression { get; set; } = expression;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessPostFix(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessPostFix(this);
}
