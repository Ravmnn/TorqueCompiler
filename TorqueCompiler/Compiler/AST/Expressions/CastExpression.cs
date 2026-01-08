using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.Compiler.AST.Expressions;




public class CastExpression(Expression expression, TypeSyntax type, Span location) : Expression(location)
{
    public Expression Expression { get; } = expression;
    public TypeSyntax Type { get; } = type;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCast(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCast(this);
}
