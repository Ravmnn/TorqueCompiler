using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler.Types;




public class FunctionTypeSyntax(TypeSyntax returnType, IReadOnlyCollection<TypeSyntax> parametersType) : PointerTypeSyntax(returnType)
{
    public TypeSyntax ReturnType => InnerType;
    public IList<TypeSyntax> ParametersType { get; } = parametersType.ToList();




    public override T Process<T>(ITypeSyntaxProcessor<T> processor)
        => processor.ProcessFunction(this);




    public override string ToString()
    {
        var parametersString = string.Join(", ", ParametersType.ItemsToString());

        return $"{ReturnType}({parametersString})";
    }
}
