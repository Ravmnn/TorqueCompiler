using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.Compiler.Symbols;




public class VariableSymbol(string name, Type? type, Span location, Scope declarationScope)
    : Symbol(name, location, declarationScope)
{
    public Type? Type { get; set; } = type;

    public bool IsParameter { get; init; }




    public VariableSymbol(SymbolSyntax symbol, Scope declarationScope)
        : this(symbol.Name, null, symbol.Location, declarationScope)
    {}




    public override string ToString()
        => $"{Type} {Name}";
}
