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
        var oldScope = scope; // TODO: check if this works
        scope = new Scope(newScope);

        var result = func();

        scope = oldScope;

        return result;
    }




    public static void ProcessInnerScope(ref Scope scope, Action action)
        => ProcessInnerScope(ref scope, scope, action);


    public static void ProcessInnerScope(ref Scope scope, Scope newScope, Action action)
    {
        var oldScope = scope; // TODO: check if this works

        scope = newScope;
        action();
        scope = oldScope;
    }




    public VariableSymbol[] GetLocalParameters()
        => Symbols.FindAll(symbol => symbol is VariableSymbol { IsParameter: true }).Cast<VariableSymbol>().ToArray();




    // TODO: TryGet__ should be the main implementation, since it's faster
    public Symbol GetSymbol(string name)
    {
        foreach (var symbol in Symbols)
            if (symbol.Name == name)
                return symbol;

        if (Parent is null)
            throw new InvalidOperationException($"Invalid symbol \"{name}\".");

        return Parent.GetSymbol(name);
    }


    public Symbol GetSymbol(LLVMValueRef reference)
    {
        foreach (var symbol in Symbols)
            if (symbol.LLVMReference == reference)
                return symbol;

        if (Parent is null)
            throw new InvalidOperationException($"Invalid symbol reference \"{reference}\"");

        return Parent.GetSymbol(reference);
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


    public Symbol? TryGetSymbol(LLVMValueRef reference)
    {
        try
        {
            return GetSymbol(reference);
        }
        catch
        {
            return null;
        }
    }




    public bool SymbolExists(string name)
        => TryGetSymbol(name) is not null;




    public Scope Global()
        => IsGlobal ? this : Parent!.Global();
}
