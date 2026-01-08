using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundGroupingExpression(GroupingExpression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public new GroupingExpression Syntax => (base.Syntax as GroupingExpression)!;

    public BoundExpression Expression { get; set; } = expression;

    public override Type? Type => Expression.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessGrouping(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);
}
