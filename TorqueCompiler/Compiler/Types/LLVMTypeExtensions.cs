using LLVMSharp.Interop;

using Torque.Compiler.Target;


namespace Torque.Compiler.Types;




public static class LLVMTypeExtensions
{
    // maybe ABISizeOfType is the incorrect method to use, so be careful

    public static int SizeOfThisInMemory(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABISizeOfType(type);


    public static int AlignmentOfThisInMemory(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABIAlignmentOfType(type);
}
