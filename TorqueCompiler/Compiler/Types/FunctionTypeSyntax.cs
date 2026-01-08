using System.Collections.Generic;


namespace Torque.Compiler.Types;




public class FunctionTypeSyntax(TypeSyntax returnType, IReadOnlyList<TypeSyntax> parametersType) : PointerTypeSyntax(returnType)
{
    public TypeSyntax ReturnType => InnerType;
    public IReadOnlyList<TypeSyntax> ParametersType { get; } = parametersType;




    public override string ToString()
    {
        var parametersString = string.Join(", ", ParametersType.ItemsToString());

        return $"{ReturnType}({parametersString})";
    }
}
