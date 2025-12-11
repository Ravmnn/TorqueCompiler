using System.Linq;


namespace Torque.Compiler;




public class TypeName(Token baseType, Token? pointerSpecifier = null)
{
    public Token BaseType { get; } = baseType;
    public Token? PointerSpecifier { get; } = pointerSpecifier;

    public virtual bool IsPointer => PointerSpecifier is not null;
    public bool IsVoid => BaseType.Lexeme == "void";




    public override string ToString()
        => $"{BaseType.Lexeme}{(IsPointer ? "*" : "")}";
}




public class FunctionTypeName(Token returnType, TypeName[] parametersType) : TypeName(returnType)
{
    public Token ReturnType => BaseType;

    public TypeName[] ParametersType { get; } = parametersType;

    public override bool IsPointer => true;




    public override string ToString()
    {
        var parameterTypesString = ParametersType.Select(parameter => parameter.ToString());
        var parameters = string.Join(", ", parameterTypesString);

        return $"{(IsVoid ? "void" : ReturnType.Lexeme)}({parameters})";
    }
}
