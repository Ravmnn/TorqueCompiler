using System.Collections.Generic;

using LLVMSharp.Interop;


namespace Torque.Compiler;




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




public class VariableSymbol(string name, Type? type, Span location, Scope declarationScope)
    : Symbol(name, location, declarationScope)
{
    public Type? Type { get; set; } = type;

    public bool IsParameter { get; init; }




    public VariableSymbol(SymbolSyntax symbol, Scope declarationScope)
        : this(symbol.Name, null, symbol.Location, declarationScope)
    {}
}




public class FunctionSymbol(string name, Type? type, IReadOnlyList<VariableSymbol> parameters, Span location, Scope declarationScope)
    : VariableSymbol(name, type, location, declarationScope)
{
    public new FunctionType? Type
    {
        get => base.Type as FunctionType;
        set => base.Type = value;
    }

    public IReadOnlyList<VariableSymbol> Parameters { get; set; } = parameters;




    public FunctionSymbol(SymbolSyntax symbol, Scope declarationScope)
        : this(symbol.Name, null, [], symbol.Location, declarationScope)
    {}
}
