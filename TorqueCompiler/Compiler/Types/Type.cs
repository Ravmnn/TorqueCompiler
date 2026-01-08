using System;

using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public enum PrimitiveType
{
    Auto,

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
    Float64
}




public abstract class Type
{
    public static Type Void => PrimitiveType.Void;




    public abstract BaseType Base { get; }


    public bool IsAuto => Base.Type == PrimitiveType.Auto; // "let" variable declarator
    public bool IsVoid => Base.Type == PrimitiveType.Void;

    public bool IsSigned => Base.Type is PrimitiveType.Int8 or PrimitiveType.Int16 or PrimitiveType.Int32 or PrimitiveType.Int64 || IsFloat;
    public bool IsUnsigned => !IsSigned;

    public bool IsFloat => Base.Type is PrimitiveType.Float16 or PrimitiveType.Float32 or PrimitiveType.Float64;
    public bool IsInteger => !IsFloat;
    public bool IsChar => Base.Type == PrimitiveType.Char;
    public bool IsBool => Base.Type == PrimitiveType.Bool;

    public bool IsBase => this is BaseType;
    public bool IsPointer => this is PointerType;
    public bool IsFunction => this is FunctionType;




    public abstract LLVMTypeRef ToLLVMType();




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
        => $"{Base.Type.PrimitiveTypeToString()}";
}
