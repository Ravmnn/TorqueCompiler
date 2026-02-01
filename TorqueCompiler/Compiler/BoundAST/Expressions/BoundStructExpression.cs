using System.Collections.Generic;

using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public record struct BoundStructMemberInitialization(SymbolSyntax Member, BoundExpression Value);


public class BoundStructExpression(IReadOnlyList<BoundStructMemberInitialization> initializationList, Expression syntax)
    : BoundExpression(syntax)
{
    public new StructExpression Syntax => (base.Syntax as StructExpression)!;

    public IReadOnlyList<BoundStructMemberInitialization> InitializationList { get; } = initializationList;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessStruct(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessStruct(this);
}
