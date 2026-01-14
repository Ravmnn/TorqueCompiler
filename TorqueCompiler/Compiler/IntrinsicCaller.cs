using System;
using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class IntrinsicCaller(LLVMModuleRef module, LLVMBuilderRef builder)
{
    public LLVMModuleRef Module { get; } = module;
    public LLVMBuilderRef Builder { get; } = builder;




    private static Dictionary<string, LLVMTypeRef> IntrinsicDeclarations { get; } = new Dictionary<string, LLVMTypeRef>
    {
        { "llvm.memset.p0i8.i64", LLVMTypeRef.CreateFunction(LLVMTypeRef.Void, [
            LLVMTypeRef.CreatePointer(LLVMTypeRef.Int8, 0), LLVMTypeRef.Int8, LLVMTypeRef.Int64, LLVMTypeRef.Int1
        ]) }
    };




    public void CallMemsetToZero(LLVMValueRef destination, LLVMValueRef sizeInBytes)
        => CallMemset(destination, Constant.Integer(0, LLVMTypeRef.Int8), sizeInBytes);

    public void CallMemset(LLVMValueRef destination, LLVMValueRef byteValue, LLVMValueRef sizeInBytes)
        => CallIntrinsic(GetMemsetName(), [destination, byteValue, sizeInBytes, Constant.Boolean(false)]);


    public static string GetMemsetName()
        => IntrinsicDeclarations.Keys.ElementAt(0);




    public void CallIntrinsic(string name, LLVMValueRef[] arguments)
    {
        var (intrinsic, type) = GetOrCreateIntrinsic(name);

        Builder.BuildCall2(type, intrinsic, arguments);
    }


    public (LLVMValueRef, LLVMTypeRef) GetOrCreateIntrinsic(string name)
    {
        var intrinsic = Module.GetNamedFunction(name);
        var type = IntrinsicDeclarations[name];

        if (intrinsic.Handle == IntPtr.Zero)
            intrinsic = Module.AddFunction(name, type);

        return (intrinsic, type);
    }
}
