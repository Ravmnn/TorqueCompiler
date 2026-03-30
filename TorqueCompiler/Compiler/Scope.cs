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
    public List<Scope> ImportedScopes { get; } = [];




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
        => TryGetSymbol(name) ?? throw new ArgumentException($"Invalid symbol \"{name}\".");



    public Symbol? TryGetSymbol(string name)
    {
        if (Symbols.Find(item => item.Name == name) is { } symbol)
            return symbol;

        return Parent?.TryGetSymbol(name) ?? TryGetSymbolInImports(name);
    }


    private Symbol? TryGetSymbolInImports(string name)
    {
        foreach (var importedScope in ImportedScopes)
            if (importedScope.TryGetSymbol(name) is { } symbolInImport)
                return symbolInImport;

        return null;
    }




    public bool SymbolExists(string name)
        => TryGetSymbol(name) is not null;


    public bool SymbolIsMultiDeclared(string name)
        => Symbols.Count(symbol => symbol.Name == name) > 1;




    public Scope Global()
        => IsGlobal ? this : Parent!.Global();
}
