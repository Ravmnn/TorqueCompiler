using System;
using System.Collections.Generic;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class Scope(Scope? parent = null)
{
    public Scope? Parent { get; } = parent;
    public bool IsGlobal => Parent is null;

    public LLVMMetadataRef? DebugMetadata { get; set; }


    public List<Symbol> Symbols { get; } = [];




    public Scope Global()
        => IsGlobal ? this : Parent!.Global();




    // TODO: TryGet__ should be the main implementation, since it's faster
    public Symbol GetSymbol(string name)
    {
        foreach (var symbol in Symbols)
            if (symbol.Name == name)
                return symbol;

        if (Parent is null)
            throw new InvalidOperationException($"Invalid identifier \"{name}\".");

        return Parent.GetSymbol(name);
    }


    public Symbol? TryGetSymbol(string name)
    {
        try
        {
            return GetSymbol(name);
        }
        catch
        {
            return null;
        }
    }




    public bool SymbolExists(string name)
        => TryGetSymbol(name) is not null;
}
