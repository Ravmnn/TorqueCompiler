using Torque.Compiler.Types;


namespace Torque.Compiler;




public static class TypeCaster
{
    public static Type? TryImplicitCast(Type from, Type to, bool forceForBaseTypes = false)
    {
        if (!CanImplicitCast(from, to, forceForBaseTypes))
            return null;

        return to;
    }


    public static bool CanImplicitCast(Type from, Type to, bool forceForBaseTypes = false)
    {
        var sameTypes = from == to;
        var bothBase = from.IsBase && to.IsBase;
        var signDiffers = from.IsSigned != to.IsSigned;
        var floatToInt = from.IsFloat && to.IsInteger;

        var rawPointerToPointer = to.IsPointer && from.IsRawPointer;
        var anyIsCompound = from.IsStruct || to.IsStruct;

        if (anyIsCompound)
            return false;

        if (rawPointerToPointer || sameTypes)
            return true;


        var typeBuilder = new TypeBuilder();
        var targetSmaller = typeBuilder.SizeOfTypeInMemory(from) > typeBuilder.SizeOfTypeInMemory(to);

        if (bothBase && forceForBaseTypes)
            return true;

        if (!bothBase)
            return false;

        if (signDiffers || floatToInt || targetSmaller)
            return false;

        return true;
    }
}
