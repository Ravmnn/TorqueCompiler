using System;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public abstract class SugarExpression(Span location) : Expression(location)
{
    public override void Process(IExpressionProcessor processor) => throw new InvalidOperationException();
    public override T Process<T>(IExpressionProcessor<T> processor) => throw new InvalidOperationException();


    public abstract Expression Process(ISugarExpressionProcessor processor);
}
