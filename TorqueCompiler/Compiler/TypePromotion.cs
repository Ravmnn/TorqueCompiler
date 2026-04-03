using System;

using Torque.Compiler.Types;


using Type = Torque.Compiler.Types.Type;


namespace Torque.Compiler;




public static class TypePromotion
{
    public static Type Promote(Type left, Type right, Func<Type, bool> validator)
    {
        var promotedType = Promote(left, right);

        return validator(promotedType) ? promotedType : Type.Error;
    }


    public static Type Promote(Type left, Type right)
    {
        var anyIsNotNumber = !left.IsNumber || !right.IsNumber;
        var anyIsNotValid = !left.IsValid || !right.IsValid;
        var equal = left == right;
        var anyIsFloat = left.IsFloat || right.IsFloat;

        if (anyIsNotNumber)
            return Type.Error;

        if (anyIsNotValid)
            return !left.IsValid ? left : right;

        if (equal)
            return left;

        if (anyIsFloat)
            return left.IsFloat ? left : right;

        return PromoteForInteger(left, right);
    }


    private static Type PromoteForInteger(Type left, Type right)
    {
        var leftSize = left.BasePrimitive.Type.SizeOfThisInMemory();
        var rightSize = right.BasePrimitive.Type.SizeOfThisInMemory();
        var signAreEqual = left.IsSigned == right.IsSigned;

        if (leftSize == rightSize)
        {
            if (signAreEqual)
                return left;

            return left.IsSigned ? left : right;
        }

        return leftSize > rightSize ? left : right;
    }
}