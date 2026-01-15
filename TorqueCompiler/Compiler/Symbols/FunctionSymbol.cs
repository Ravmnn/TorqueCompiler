using System.Collections.Generic;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.Compiler.Symbols;




public class FunctionSymbol(string name, Type? type, IReadOnlyList<VariableSymbol> parameters, Span location, Scope declarationScope)
    : VariableSymbol(name, type, location, declarationScope)
{
    public new FunctionType? Type
    {
        get => base.Type as FunctionType;
        set => base.Type = value;
    }

    public IReadOnlyList<VariableSymbol> Parameters { get; set; } = parameters;

    public bool IsExternal { get; set; }




    public FunctionSymbol(SymbolSyntax symbol, Scope declarationScope)
        : this(symbol.Name, null, [], symbol.Location, declarationScope)
    {}




    public override string ToString()
    {
        var parametersString = string.Join(", ", Parameters.ItemsToString());

        return $"{Type?.ReturnType} {Name}({parametersString})";
    }
}
