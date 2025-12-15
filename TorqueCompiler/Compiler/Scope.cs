using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class Scope(Scope? parent = null)
{
    public Scope? Parent { get; } = parent;
    public bool IsGlobal => Parent is null;

    public LLVMMetadataRef? DebugMetadata { get; set; }


    public List<Symbol> Symbols { get; } = [];




    public static T ProcessInnerScope<T>(ref Scope scope, Func<T> func)
        => ProcessInnerScope(ref scope, scope, func);


    public static T ProcessInnerScope<T>(ref Scope scope, Scope newScope, Func<T> func)
    {
        var oldScope = scope;
        scope = new Scope(newScope);

        var result = func();

        scope = oldScope;

        return result;
    }




    public static void ProcessInnerScope(ref Scope scope, Scope newScope, Action action)
    {
        var oldScope = scope;

        scope = newScope;
        action();
        scope = oldScope;
    }




    public IReadOnlyList<VariableSymbol> GetLocalParameters()
        => Symbols.FindAll(symbol => symbol is VariableSymbol { IsParameter: true }).Cast<VariableSymbol>().ToArray();




    public Symbol GetSymbol(string name)
        => TryGetSymbol(name) ?? throw new InvalidOperationException($"Invalid symbol \"{name}\".");


    public Symbol GetSymbol(LLVMValueRef reference)
        => TryGetSymbol(reference) ?? throw new InvalidOperationException($"Invalid symbol reference \"{reference}\"");




    public Symbol? TryGetSymbol(string name)
    {
        foreach (var symbol in Symbols)
            if (symbol.Name == name)
                return symbol;

        return Parent?.TryGetSymbol(name);
    }


    public Symbol? TryGetSymbol(LLVMValueRef reference)
    {
        foreach (var symbol in Symbols)
            if (symbol.LLVMReference == reference)
                return symbol;

        return Parent?.TryGetSymbol(reference);
    }




    public bool SymbolExists(string name)
        => TryGetSymbol(name) is not null;




    public Scope Global()
        => IsGlobal ? this : Parent!.Global();
}
