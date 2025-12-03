using System;
using System.Collections.Generic;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class Scope<T>(Scope<T>? parent = null, LLVMMetadataRef? debugReference = null)
    : List<T> where T : IIdentifier
{
    public Scope<T>? Parent { get; } = parent;

    public LLVMMetadataRef? DebugReference { get; set; } = debugReference;


    public bool IsGlobal => Parent is null;




    public T GetIdentifier(string name)
    {
        foreach (var identifier in this)
            if (identifier.Name == name)
                return identifier;

        if (Parent is null)
            throw new InvalidOperationException($"Invalid identifier \"{name}\".");

        return Parent.GetIdentifier(name);
    }


    public T? TryGetIdentifier(string name)
    {
        try
        {
            return GetIdentifier(name);
        }
        catch
        {
            return default;
        }
    }




    public bool IdentifierExists(string name)
        => TryGetIdentifier(name) is not null;
}
