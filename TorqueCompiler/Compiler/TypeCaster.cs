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
        var targetSmaller = from.SizeOfTypeInMemory() > to.SizeOfTypeInMemory();

        var rawPointerToPointer = to.IsPointer && from.IsRawPointer;
        var anyIsCompound = from.IsCompound || to.IsCompound;

        if (anyIsCompound)
            return false;

        if (rawPointerToPointer || sameTypes)
            return true;

        if (bothBase && forceForBaseTypes)
            return true;

        if (!bothBase)
            return false;

        if (signDiffers || floatToInt || targetSmaller)
            return false;

        return true;
    }
}
