using System;
using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




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
