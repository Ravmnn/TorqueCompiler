using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class LiteralExpression(Token token, Span location) : Expression(location)
{
    public Token Token { get; } = token;
    public object Value => Token.Value!;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLiteral(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);
}
