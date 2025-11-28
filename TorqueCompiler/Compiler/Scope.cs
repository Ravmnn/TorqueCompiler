using System;
using System.Collections.Generic;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public readonly struct Identifier(LLVMValueRef address, LLVMTypeRef type)
{
    public LLVMValueRef Address { get; } = address;
    public LLVMTypeRef Type { get; } = type;
}


public class Scope(Scope? parent = null) : List<Identifier>
{
    public Scope? Parent { get; } = parent;


    public bool IsGlobal => Parent is null;




    public Identifier GetIdentifier(string name)
    {
        foreach (var identifier in this)
            if (identifier.Address.Name == name)
                return identifier;

        return Parent?.GetIdentifier(name) ?? throw new InvalidOperationException("Invalid identifier.");
    }


    public Identifier? TryGetIdentifier(string name)
    {
        try
        {
            return GetIdentifier(name);
        }
        catch
        {
            return null;
        }
    }




    public bool IdentifierExists(string name)
        => TryGetIdentifier(name) is not null;
}
