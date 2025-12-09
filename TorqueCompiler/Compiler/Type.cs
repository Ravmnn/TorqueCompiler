using LLVMSharp.Interop;


namespace Torque.Compiler;




public readonly record struct Type(PrimitiveType BaseType, bool IsPointer = false)
{
    public static implicit operator PrimitiveType(Type type) => type.BaseType;
    public static implicit operator Type(PrimitiveType type) => new Type(type);


    public override string ToString()
        => $"{BaseType.PrimitiveToString()}{(IsPointer ? "*" : "")}";
}




public readonly struct TypeName(Token baseType, Token? pointerSpecifier = null)
{
    public Token BaseType { get; } = baseType;
    public Token? PointerSpecifier { get; } = pointerSpecifier;

    public bool IsPointer => PointerSpecifier is not null;


    public override string ToString()
        => $"{BaseType.Lexeme}{(IsPointer ? "*" : "")}";
}




public static class TypeExtensions
{
    public static LLVMTypeRef TypeToLLVMType(this Type type)
    {
        var llvmBaseType = type.BaseType.PrimitiveToLLVMType();

        return type switch
        {
            _ when type.IsPointer => LLVMTypeRef.CreatePointer(llvmBaseType, 0),
            _ => llvmBaseType
        };
    }




    public static int SizeOfThis(this Type type, LLVMTargetDataRef targetData)
        => (int)targetData.ABISizeOfType(type.TypeToLLVMType());
}
