using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class FunctionType(Type returnType, IReadOnlyList<Type> parametersType) : PointerType(returnType)
{
    public override BasePrimitiveType BasePrimitive => ReturnType.BasePrimitive;

    public Type ReturnType => Type;
    public IReadOnlyList<Type> ParametersType { get; } = parametersType;




    public override T Process<T>(ITypeProcessor<T> processor)
        => processor.ProcessFunction(this);




    public override string ToString()
    {
        var parametersString = string.Join(", ", ParametersType.ItemsToString());

        return $"{ReturnType}({parametersString})";
    }




    protected override bool Equals(Type other)
    {
        if (other is not FunctionType otherType)
            return false;

        return ReturnType == otherType.ReturnType && ParametersType.SequenceEqual(otherType.ParametersType);
    }


    public override int GetHashCode()
        => HashCode.Combine((int)BasePrimitive.Type, ParametersType);
}
