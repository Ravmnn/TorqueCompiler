using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public abstract class Type
{
    public static Type Void => PrimitiveType.Void;




    public abstract BaseType Base { get; }


    public bool IsVoid => Base.Type == PrimitiveType.Void;

    public bool IsSigned => Base.Type is PrimitiveType.Int8 or PrimitiveType.Int16 or PrimitiveType.Int32 or PrimitiveType.Int64 || IsFloat;
    public bool IsUnsigned => !IsSigned; // TODO: use IsUnsigned (and IsInteger in some cases to improve readability

    public bool IsFloat => Base.Type is PrimitiveType.Float16 or PrimitiveType.Float32 or PrimitiveType.Float64;
    public bool IsInteger => !IsFloat;

    public bool IsBase => this is BaseType;
    public bool IsPointer => this is PointerType;
    public bool IsFunction => this is FunctionType;




    public static implicit operator PrimitiveType(Type type) => type.Base.Type;
    public static implicit operator Type(PrimitiveType type) => new BaseType(type);


    public static bool operator ==(Type left, Type right) => left.Equals((object)right);
    public static bool operator !=(Type left, Type right) => !(left == right);


    public override bool Equals(object? other)
    {
        if (other is not Type type)
            return false;

        return Equals(type) && type.Equals(this);
    }


    protected virtual bool Equals(Type other)
        => Base.Type == other.Base.Type;


    public override int GetHashCode()
        => HashCode.Combine((int)Base.Type);




    public override string ToString()
        => $"{Base.Type.PrimitiveToString()}";
}




public class BaseType(PrimitiveType type) : Type
{
    public override BaseType Base => this;


    public PrimitiveType Type { get; } = type;
}




public class PointerType(Type type) : Type
{
    public override BaseType Base => Type.Base;


    public Type Type { get; } = type;




    protected override bool Equals(Type other)
    {
        if (other is not PointerType otherType)
            return false;

        return Type == otherType.Type;
    }




    public override string ToString()
        => $"{Type}*";
}




public class FunctionType(Type returnType, IReadOnlyList<Type> parametersType) : Type
{
    public override BaseType Base => ReturnType.Base;


    public Type ReturnType { get; } = returnType;

    public IReadOnlyList<Type> ParametersType { get; } = parametersType;




    public override string ToString()
    {
        var parameterTypesString = ParametersType.Select(parameter => parameter.ToString());
        var parameters = string.Join(", ", parameterTypesString);

        return $"{(IsVoid ? "void" : ReturnType)}({parameters})";
    }




    protected override bool Equals(Type other)
    {
        if (other is not FunctionType otherType)
            return false;

        return ReturnType == otherType.ReturnType && ParametersType.SequenceEqual(otherType.ParametersType);
    }


    public override int GetHashCode()
        => HashCode.Combine((int)Base.Type, ParametersType);
}




public static class TypeExtensions
{
    public static IReadOnlyList<LLVMTypeRef> TypesToLLVMTypes(this IReadOnlyList<Type> types)
        => types.Select(type => type.TypeToLLVMType()).ToArray();


    public static LLVMTypeRef TypeToLLVMType(this Type type) => type switch
    {
        BaseType baseType => baseType.Type.PrimitiveToLLVMType(),
        PointerType pointerType => PointerTypeToLLVMType(pointerType),
        FunctionType functionType => FunctionTypeToLLVMType(functionType),

        _ => throw new UnreachableException()
    };


    public static LLVMTypeRef PointerTypeToLLVMType(this PointerType pointerType)
        => LLVMTypeRef.CreatePointer(pointerType.Type.TypeToLLVMType(), 0);


    public static LLVMTypeRef FunctionTypeToLLVMType(this FunctionType functionType, bool pointer = true)
    {
        var llvmReturnType = functionType.ReturnType.TypeToLLVMType();
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




    public static int SizeOfThisInMemory(this Type type, LLVMTargetDataRef targetData)
        => (int)targetData.ABISizeOfType(type.TypeToLLVMType());
}
