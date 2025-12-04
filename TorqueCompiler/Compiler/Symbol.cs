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




public class IdentifierSymbol(string name, PrimitiveType? type, TokenLocation location, Scope declarationScope)
    : Symbol(name, location, declarationScope)
{
    public PrimitiveType? Type { get; set; } = type;
}




public class FunctionSymbol(string name, PrimitiveType? returnType, PrimitiveType[]? parameters, TokenLocation location, Scope declarationScope)
    : Symbol(name, location, declarationScope)
{
    public PrimitiveType? ReturnType { get; set; } = returnType;
    public PrimitiveType[]? Parameters { get; set; } = parameters;
}
