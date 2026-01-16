using LLVMSharp.Interop;

using Torque.Compiler.Target;


namespace Torque.Compiler.Types;




public static class LLVMTypeExtensions
{
    public static int SizeOfThisInMemory(this LLVMTypeRef type, LLVMTargetDataRef? targetData = null)
        => (int)TargetMachine.GetDataLayoutOfOrGlobal(targetData).ABISizeOfType(type);
}
