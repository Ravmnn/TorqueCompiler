using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;


namespace Torque.Compiler.AST.Expressions;




public record struct StructMemberInitialization(SymbolSyntax Member, Expression Value);


public class StructExpression(SymbolSyntax symbol, IReadOnlyList<StructMemberInitialization> initializationList, Span location)
    : Expression(location)
{
    public SymbolSyntax Symbol { get; } = symbol;
    public IList<StructMemberInitialization> InitializationList { get; } = initializationList.ToList();




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessStruct(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessStruct(this);
}
