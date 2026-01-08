using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler.Types;




public class FunctionType(Type returnType, IReadOnlyList<Type> parametersType) : PointerType(returnType)
{
    public override BaseType Base => ReturnType.Base;

    public Type ReturnType => Type;
    public IReadOnlyList<Type> ParametersType { get; } = parametersType;




    public override LLVMTypeRef ToLLVMType()
        => LLVMTypeRef.CreatePointer(ToRawLLVMType(), 0);


    public LLVMTypeRef ToRawLLVMType()
    {
        var llvmReturnType = ReturnType.ToLLVMType();
        var llvmParametersType = ParametersType.TypesToLLVMTypes();
        var llvmFunctionType = LLVMTypeRef.CreateFunction(llvmReturnType, llvmParametersType.ToArray());

        return llvmFunctionType;
    }




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
        => HashCode.Combine((int)Base.Type, ParametersType);
}
