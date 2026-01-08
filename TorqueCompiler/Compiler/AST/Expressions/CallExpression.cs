using System.Collections.Generic;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class CallExpression(Expression callee, IReadOnlyList<Expression> arguments, Span location) : Expression(location)
{
    public Expression Callee { get; } = callee;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCall(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCall(this);
}
