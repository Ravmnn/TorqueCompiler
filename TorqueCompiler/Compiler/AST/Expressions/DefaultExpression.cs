using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.Compiler.AST.Expressions;




public class DefaultExpression(TypeSyntax typeSyntax, Span location) : Expression(location)
{
    public TypeSyntax TypeSyntax { get; } = typeSyntax;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessDefault(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessDefault(this);
}
