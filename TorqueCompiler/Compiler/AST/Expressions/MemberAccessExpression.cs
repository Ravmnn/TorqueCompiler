using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Expressions;




public class MemberAccessExpression(Expression compound, SymbolSyntax member, Span location) : Expression(location)
{
    public Expression Compound { get; set; } = compound;
    public SymbolSyntax Member { get; } = member;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessMemberAccess(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessMemberAccess(this);
}
