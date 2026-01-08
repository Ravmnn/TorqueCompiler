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

        var target = TargetFromTripleOrThrow(triple);

        Triple = triple;
        Target = target;
        Machine = CreateDefaultTargetMachine(triple, target);
        DataLayout = Machine.CreateTargetDataLayout();
        StringDataLayout = DataLayoutToStringOrThrow(DataLayout);
    }


    private static unsafe string DataLayoutToStringOrThrow(LLVMTargetDataRef dataLayout)
    {
        var ptr = LLVM.CopyStringRepOfTargetData(dataLayout);
        return Marshal.PtrToStringAnsi((IntPtr)ptr) ?? throw new InvalidOperationException("Couldn't create data layout");
    }


    private static LLVMTargetMachineRef CreateDefaultTargetMachine(string triple, LLVMTargetRef target)
        => target.CreateTargetMachine(
            triple, "generic", "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocPIC, LLVMCodeModel.LLVMCodeModelDefault
        );


    private static LLVMTargetRef TargetFromTripleOrThrow(string triple)
    {
        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out _))
            throw new InvalidOperationException("LLVM doesn't support this target");

        return target;
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
