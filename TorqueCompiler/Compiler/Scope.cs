using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Torque.Compiler.Symbols;


namespace Torque.Compiler;




public class Scope(Scope? parent = null)
{
    public Scope? Parent { get; } = parent;
    public bool IsGlobal => Parent is null;

    public LLVMMetadataRef? DebugMetadata { get; set; }


    public List<Symbol> Symbols { get; } = [];




    public static T ForInnerScopeDo<T>(ref Scope scope, Func<T> func)
        => ForInnerScopeDo(ref scope, scope, func);


    public static T ForInnerScopeDo<T>(ref Scope scope, Scope newScope, Func<T> func)
    {
        var oldScope = scope;
        scope = new Scope(newScope);

        var result = func();

        scope = oldScope;

        return result;
    }


    public static void ForInnerScopeDo(ref Scope scope, Scope newScope, Action action)
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
        if (Symbols.Find(item => item.Name == name) is { } symbol)
            return symbol;

        return Parent?.TryGetSymbol(name);
    }


    public Symbol? TryGetSymbol(LLVMValueRef reference)
    {
        if (Symbols.Find(item => item.LLVMReference == reference) is { } symbol)
            return symbol;

        return Parent?.TryGetSymbol(reference);
    }




    public bool SymbolExists(string name)
        => TryGetSymbol(name) is not null;




    public Scope Global()
        => IsGlobal ? this : Parent!.Global();
}
