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




public class VariableSymbol(string name, Type? type, TokenLocation location, Scope declarationScope)
    : Symbol(name, location, declarationScope)
{
    public Type? Type { get; set; } = type;

    public bool IsParameter { get; init; }


    public VariableSymbol(Token symbol, Scope declarationScope)
        : this(symbol.Lexeme, null, symbol.Location, declarationScope)
    {}
}




public class FunctionSymbol(string name, Type? type, VariableSymbol[] parameters, TokenLocation location, Scope declarationScope)
    : VariableSymbol(name, type, location, declarationScope)
{
    public new FunctionType? Type
    {
        get => base.Type as FunctionType;
        set => base.Type = value;
    }

    public VariableSymbol[] Parameters { get; set; } = parameters;


    public FunctionSymbol(Token symbol, Scope declarationScope)
        : this(symbol.Lexeme, null, [], symbol.Location, declarationScope)
    {}
}
