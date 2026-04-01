using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;

using Torque.Compiler.Symbols;


namespace Torque.Compiler;




public class Scope(Scope? parent = null) : ImportableStorage<Symbol>
{
    public Scope? Parent { get; } = parent;
    public bool IsGlobal => Parent is null;

    public LLVMMetadataRef? DebugMetadata { get; set; }




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




    public override Symbol? TryGet(string symbol)
    {
        var found = Items.FirstOrDefault(Predicate(symbol));

        if (found is not null)
            return found;

        return Parent?.TryGet(symbol) ?? TryGetFromImportedStorages(symbol);
    }




    public IReadOnlyList<VariableSymbol> GetLocalParameters()
        => [.. Items.FindAll(symbol => symbol is VariableSymbol { IsParameter: true }).Cast<VariableSymbol>()];





    public Scope Global()
        => IsGlobal ? this : Parent!.Global();
}
