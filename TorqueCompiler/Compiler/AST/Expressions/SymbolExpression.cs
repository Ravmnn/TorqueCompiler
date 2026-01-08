using Torque.Compiler.Symbols;


namespace Torque.Compiler.AST.Expressions;




public class SymbolExpression(SymbolSyntax symbol) : Expression(symbol.Location)
{
    public SymbolSyntax Symbol { get; } = symbol;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessSymbol(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);
}
