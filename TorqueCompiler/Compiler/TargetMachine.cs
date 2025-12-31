using System;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class TargetMachine
{
    private static bool s_initialized;


    public static TargetMachine? Global { get; private set; }


    public string Triple { get; }

    public LLVMTargetRef Target { get; }
    public LLVMTargetMachineRef Machine { get; }
    public LLVMTargetDataRef DataLayout { get; }
    public string StringDataLayout { get; }




    public unsafe TargetMachine(string triple)
    {
        InitLLVM();

        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out _))
            throw new InvalidOperationException("LLVM doesn't support this target.");

        Triple = triple;

        Target = target;
        Machine = target.CreateTargetMachine(
            triple, "generic", "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocPIC,
            LLVMCodeModel.LLVMCodeModelDefault
        );

        DataLayout = Machine.CreateTargetDataLayout();

        var ptr = LLVM.CopyStringRepOfTargetData(DataLayout);
        StringDataLayout = Marshal.PtrToStringAnsi((IntPtr)ptr) ?? throw new InvalidOperationException("Couldn't create data layout");
    }


    private static void InitLLVM()
    {
        if (s_initialized)
            return;

        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();

        s_initialized = true;
    }




    public static void SetGlobal(string triple)
        => Global = new TargetMachine(triple);


    public static LLVMTargetDataRef GetDataLayoutOfOrGlobal(LLVMTargetDataRef? dataLayout)
        => dataLayout ?? Global!.DataLayout;
}
