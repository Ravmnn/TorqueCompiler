using LLVMSharp.Interop;


namespace Torque.Compiler;




public abstract class Symbol(string name, TokenLocation location, Scope declarationScope)
{
    public string Name { get; } = name;
    public TokenLocation Location { get; } = location;
    public Scope DeclarationScope { get; } = declarationScope;


    public LLVMValueRef? LLVMReference { get; set; }
    public LLVMTypeRef? LLVMType { get; set; }
    public LLVMMetadataRef? LLVMDebugMetadata { get; set; }
}




public class ValueSymbol(string name, Type? type, TokenLocation location, Scope declarationScope)
    : Symbol(name, location, declarationScope)
{
    public Type? Type { get; set; } = type;
}




public class FunctionSymbol(string name, Type? returnType, Type[]? parameters, TokenLocation location, Scope declarationScope)
    : ValueSymbol(name, returnType, location, declarationScope)
{
    public Type? ReturnType
    {
        get => Type;
        set => Type = value;
    }

    public Type[]? Parameters { get; set; } = parameters;
}
