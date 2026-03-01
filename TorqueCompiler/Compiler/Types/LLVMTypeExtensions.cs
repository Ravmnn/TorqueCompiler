using LLVMSharp.Interop;

using Torque.Compiler.Target;


namespace Torque.Compiler.Types;




public static class LLVMTypeExtensions
{
    // maybe ABISizeOfType is the incorrect method to use, so be careful

    public static int SizeOfThisInMemory(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABISizeOfType(type);

    public static int SizeOfThisInMemoryAsBits(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => type.SizeOfThisInMemory(targetData) * 8;


    public static int AlignmentOfThisInMemory(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABIAlignmentOfType(type);

    public static int AlignmentOfThisInMemoryAsBits(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => type.AlignmentOfThisInMemory(targetData) * 8;
}
