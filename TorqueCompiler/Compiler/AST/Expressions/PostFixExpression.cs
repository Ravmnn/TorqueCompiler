using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class PostFixExpression(Expression expression, TokenType @operator, Span location) : Expression(location)
{
    public Expression Expression { get; set; } = expression;
    public TokenType Operator { get; } = @operator;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessPostFix(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessPostFix(this);
}
