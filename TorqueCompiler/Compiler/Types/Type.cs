using System;

using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public enum PrimitiveType
{
    Void,
    PtrSize,
    Bool,
    Char,
    Int8,
    Int16,
    Int32,
    Int64,
    UInt8,
    UInt16,
    UInt32,
    UInt64,
    Float16,
    Float32,
    Float64,

    Struct
}




public abstract class Type
{
    public static Type Void => PrimitiveType.Void;




    public abstract BasePrimitiveType BasePrimitive { get; }


    public bool IsVoid => IsBase && BasePrimitive.Type == PrimitiveType.Void;

    public bool IsSigned => IsBase && BasePrimitive.Type is PrimitiveType.Int8 or PrimitiveType.Int16 or PrimitiveType.Int32 or PrimitiveType.Int64 || IsFloat;
    public bool IsUnsigned => IsBase && IsInteger && !IsSigned;

    public bool IsFloat => IsBase && BasePrimitive.Type is PrimitiveType.Float16 or PrimitiveType.Float32 or PrimitiveType.Float64;
    public bool IsInteger => (IsBase || IsPointer) && !IsFloat && !IsCompound;
    public bool IsChar => IsBase && BasePrimitive.Type == PrimitiveType.Char;
    public bool IsBool => IsBase && BasePrimitive.Type == PrimitiveType.Bool;

    public bool IsString => this is PointerType { Type.IsChar: true };

    public bool IsBase => this is BasePrimitiveType;
    public bool IsPointer => this is PointerType;
    public bool IsRawPointer => this is PointerType pointerType && pointerType.Type == PrimitiveType.UInt8;
    public bool IsFunction => this is FunctionType;
    public bool IsCompound => this is StructType;




    public abstract LLVMTypeRef ToLLVMType();




    public static implicit operator Type(PrimitiveType type) => new BasePrimitiveType(type);


    public static bool operator ==(Type left, Type right) => left.Equals((object)right);
    public static bool operator !=(Type left, Type right) => !(left == right);


    public override bool Equals(object? other)
    {
        if (other is not Type type)
            return false;

        return Equals(type) && type.Equals(this);
    }


    protected virtual bool Equals(Type other)
        => BasePrimitive.Type == other.BasePrimitive.Type;


    public override int GetHashCode()
        => HashCode.Combine((int)BasePrimitive.Type);




    public override string ToString()
        => $"{BasePrimitive.Type.PrimitiveTypeToString()}";
}
