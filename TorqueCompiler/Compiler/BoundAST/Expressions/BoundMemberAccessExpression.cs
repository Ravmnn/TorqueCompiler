using Torque.Compiler.AST.Expressions;
using Torque.Compiler.Symbols;
using Torque.Compiler.Types;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundMemberAccessExpression(Expression syntax, BoundExpression compound, SymbolSyntax member) : BoundExpression(syntax)
{
    public new MemberAccessExpression Syntax => (base.Syntax as MemberAccessExpression)!;

    public BoundExpression Compound { get; set; } = compound;
    public SymbolSyntax Member { get; } = member;





    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessMemberAccess(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessMemberAccess(this);
}
