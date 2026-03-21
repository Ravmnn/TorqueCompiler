using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class SugarArrowExpression(Expression pointer, SymbolSyntax member, Span location) : SugarExpression(location)
{
    public Expression Pointer { get; } = pointer;
    public SymbolSyntax Member { get; } = member;




    public override Expression Process(ISugarExpressionProcessor processor)
        => processor.ProcessArrow(this);
}
