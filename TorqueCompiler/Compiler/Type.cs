using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




// TODO: make type infinite recursive
public class Type(PrimitiveType baseType, bool isPointer = false)
{
    public static Type Void => PrimitiveType.Void;




    public PrimitiveType BaseType { get; } = baseType;
    public bool IsPointer { get; } = isPointer;
    public bool IsVoid => BaseType == PrimitiveType.Void;
    public bool IsUnsigned => BaseType is PrimitiveType.UInt8 or PrimitiveType.UInt16 or PrimitiveType.UInt32
                                        or PrimitiveType.UInt64 or PrimitiveType.Char or PrimitiveType.Bool;




    public static implicit operator PrimitiveType(Type type) => type.BaseType;
    public static implicit operator Type(PrimitiveType type) => new Type(type);


    public override string ToString()
        => $"{BaseType.PrimitiveToString()}{(IsPointer ? "*" : "")}";




    public static bool operator ==(Type left, Type right) => left.Equals((object)right);
    public static bool operator !=(Type left, Type right) => !(left == right);


    public override bool Equals(object? other)
    {
        if (other is not Type type)
            return false;

        return Equals(type) && type.Equals(this);
    }


    protected virtual bool Equals(Type other)
        => BaseType == other.BaseType && IsPointer == other.IsPointer;


    public override int GetHashCode()
        => HashCode.Combine((int)BaseType, IsPointer);
}




public class FunctionType(Type returnType, IReadOnlyList<Type> parametersType)
    : Type(returnType.BaseType, true)
{
    public Type ReturnType => BaseType;

    public IReadOnlyList<Type> ParametersType { get; } = parametersType;




    public override string ToString()
    {
        var parameterTypesString = ParametersType.Select(parameter => parameter.ToString());
        var parameters = string.Join(", ", parameterTypesString);

        return $"{(IsVoid ? "void" : ReturnType.ToString())}({parameters})";
    }




    protected override bool Equals(Type other)
    {
        if (other is not FunctionType otherType)
            return false;

        return ReturnType == otherType.ReturnType && ParametersType.SequenceEqual(otherType.ParametersType);
    }


    public override int GetHashCode()
        => HashCode.Combine((int)BaseType, ParametersType);
}




public static class TypeExtensions
{
    public static IReadOnlyList<LLVMTypeRef> TypesToLLVMTypes(this IReadOnlyList<Type> types)
        => (from type in types select type.TypeToLLVMType()).ToArray();


    public static LLVMTypeRef TypeToLLVMType(this Type type)
    {
        var llvmBaseType = type.BaseType.PrimitiveToLLVMType();

        return type switch
        {
            FunctionType functionType => FunctionTypeToLLVMType(functionType),

            _ when type.IsPointer => LLVMTypeRef.CreatePointer(llvmBaseType, 0),
            _ => llvmBaseType
        };
    }


    public static LLVMTypeRef FunctionTypeToLLVMType(this FunctionType functionType, bool pointer = true)
    {
        var llvmReturnType = functionType.BaseType.PrimitiveToLLVMType();
        var llvmParametersType = functionType.ParametersType.TypesToLLVMTypes();
        var llvmFunctionType = LLVMTypeRef.CreateFunction(llvmReturnType, llvmParametersType.ToArray());

        // "pointer" determines whether we want a pointer to the function: ptr to "i32 (i32, i32)"
        // (this is used by a variable to store the function)
        // or the function type itself: "i32 (i32, i32)"
        // (this is used by LLVM everytime we want to do any operation with that function, like creating one,
        // calling one... This works that way now because LLVM uses only opaque pointers, so we have to
        // store the real type of the pointer somewhere)

        return pointer ? LLVMTypeRef.CreatePointer(llvmFunctionType, 0) : llvmFunctionType;
    }




    public static int SizeOfThis(this Type type, LLVMTargetDataRef targetData)
        => type switch
        {
            _ when type.BaseType == PrimitiveType.Void => 0,
            _ => (int)targetData.ABISizeOfType(type.TypeToLLVMType())
        };
}
