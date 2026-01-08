using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundSymbolExpression(SymbolExpression syntax, VariableSymbol symbol) : BoundExpression(syntax)
{
    public new SymbolExpression Syntax => (base.Syntax as SymbolExpression)!;

    public override Type Type => Symbol.Type!;
    public VariableSymbol Symbol { get; } = symbol;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessSymbol(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);
}
