using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class SugarNullptrExpression(Span location) : SugarExpression(location)
{
    public override Expression Process(ISugarExpressionProcessor processor)
        => processor.ProcessNullptr(this);
}
