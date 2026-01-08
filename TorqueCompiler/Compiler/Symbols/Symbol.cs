using LLVMSharp.Interop;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.Symbols;




public abstract class Symbol(string name, Span location, Scope declarationScope)
{
    public string Name { get; } = name;
    public Span Location { get; } = location;
    public Scope DeclarationScope { get; } = declarationScope;


    public LLVMValueRef? LLVMReference { get; set; }
    public LLVMTypeRef? LLVMType { get; set; }
    public LLVMMetadataRef? LLVMDebugMetadata { get; set; }




    public Symbol(SymbolSyntax symbol, Scope declarationScope) : this(symbol.Name, symbol.Location, declarationScope)
    { }




    public void SetLLVMProperties(LLVMValueRef reference, LLVMTypeRef type, LLVMMetadataRef? debugMetadata)
    {
        LLVMReference = reference;
        LLVMType = type;
        LLVMDebugMetadata = debugMetadata;
    }
}
