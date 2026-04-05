using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundPreFixExpression(Expression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public new PreFixExpression Syntax => (base.Syntax as PreFixExpression)!;

    public BoundExpression Expression { get; set; } = expression;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessPreFix(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessPreFix(this);
}

