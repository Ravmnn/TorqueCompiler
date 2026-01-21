using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class LiteralExpression(object value, Span location) : Expression(location)
{
    public object Value { get; } = value;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLiteral(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);
}
