using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class IndexingExpression(Expression pointer, Expression index, Span location) : Expression(location)
{
    public Expression Pointer { get; } = pointer;
    public Expression Index { get; } = index;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessIndexing(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessIndexing(this);
}
