using System;
using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Symbols;


namespace Torque.Compiler;




public abstract class ImportableStorage<T> where T : IName
{
    public List<T> Items { get; } = [];
    public List<ImportableStorage<T>> ImportedStorages { get; } = [];




    public TItem Get<TItem>(string symbol) where TItem : T
        => TryGet<TItem>(symbol) ?? throw NotFound<TItem>(symbol);


    public T Get(string symbol)
        => TryGet(symbol) ?? throw NotFound<T>(symbol);


    private static Exception NotFound<TItem>(string symbol) where TItem : T
        => new KeyNotFoundException($"Symbol '{symbol}' of type '{typeof(TItem).Name}' not found in this storage or any of its imported storages.");




    public TItem? TryGet<TItem>(string symbol) where TItem : T
        => (TItem?)TryGet(symbol);


    public virtual T? TryGet(string symbol)
    {
        var found = Items.FirstOrDefault(Predicate(symbol));

        if (found is not null)
            return found;

        return TryGetFromImportedStorages(symbol);
    }


    protected virtual T? TryGetFromImportedStorages(string symbol)
    {
        foreach (var importedManager in ImportedStorages)
            if (importedManager.TryGet(symbol) is { } item)
                return item;

        return default;
    }




    public bool Exists<TItem>(string symbol) where TItem : T
        => TryGet<TItem>(symbol) is not null;


    public bool Exists(string symbol)
        => TryGet(symbol) is not null;


    public bool ExistsMultiple(string symbol)
        => Items.Count(Predicate(symbol)) > 1;




    protected static Func<T, bool> Predicate(string symbol)
        => item => item.Name == symbol;
}
