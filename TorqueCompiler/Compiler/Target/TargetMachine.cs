using System;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler.Target;




public class TargetMachine
{
    public static TargetMachine? Global { get; private set; }


    public string Triple { get; }

    public LLVMTargetRef Target { get; }
    public LLVMTargetMachineRef Machine { get; }
    public LLVMTargetDataRef DataLayout { get; }
    public string StringDataLayout { get; }




    public TargetMachine(string triple)
    {
        var target = TargetFromTripleOrThrow(triple);

        Triple = triple;
        Target = target;
        Machine = CreateDefaultTargetMachine(triple, target);
        DataLayout = Machine.CreateTargetDataLayout();
        StringDataLayout = DataLayoutToStringOrThrow(DataLayout);
    }


    private static LLVMTargetRef TargetFromTripleOrThrow(string triple)
    {
        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out _))
            throw new InvalidOperationException($"LLVM doesn't support the target \"{triple}\"");

        return target;
    }


    private static LLVMTargetMachineRef CreateDefaultTargetMachine(string triple, LLVMTargetRef target)
        => target.CreateTargetMachine(
            triple,
            "generic",
            "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault,
            LLVMRelocMode.LLVMRelocDefault,
            LLVMCodeModel.LLVMCodeModelDefault
        );


    private static unsafe string DataLayoutToStringOrThrow(LLVMTargetDataRef dataLayout)
    {
        var ptr = LLVM.CopyStringRepOfTargetData(dataLayout);
        return Marshal.PtrToStringAnsi((IntPtr)ptr) ?? throw new InvalidOperationException("Couldn't create data layout");
    }




    public static void SetGlobalTarget(string triple)
        => Global = new TargetMachine(triple);


    public static LLVMTargetDataRef GetDataLayoutOfOrGlobal(LLVMTargetDataRef? dataLayout)
        => dataLayout ?? Global!.DataLayout;
}
