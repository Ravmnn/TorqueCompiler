namespace Torque.Compiler.Types;




public interface ITypeProcessor<out T>
{
    T Process(Type type);

    T ProcessPrimitive(BasePrimitiveType type);
    T ProcessPointer(PointerType type);
    T ProcessArray(ArrayType type);
    T ProcessFunction(FunctionType type);
    T ProcessStruct(StructType type);
}
