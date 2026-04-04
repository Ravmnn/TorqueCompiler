using LLVMSharp.Interop;


namespace Torque.Compiler;




public static class LLVMInitalize
{
    public static void InitializeAll()
    {
        LLVM.InitializeAllTargetInfos();
        LLVM.InitializeAllTargets();
        LLVM.InitializeAllTargetMCs();
    }
}
