using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public abstract class TypeName
{
    public abstract BaseTypeName Base { get; }


    public bool IsAuto => Base.TypeToken.Lexeme == "let";
    public bool IsVoid => Base.TypeToken.Lexeme == "void";
    public bool IsBase => this is BaseTypeName;
    public bool IsPointer => this is PointerTypeName;
    public bool IsArray => this is ArrayTypeName;
    public bool IsFunction => this is FunctionTypeName;




    public override string ToString()
        => Base.TypeToken.Lexeme;
}




public class BaseTypeName(Token typeToken) : TypeName
{
    public override BaseTypeName Base => this;


    public Token TypeToken { get; } = typeToken;
}




public class PointerTypeName(TypeName type, Token? pointerSpecifier = null) : TypeName
{
    public override BaseTypeName Base => Type.Base;


    public TypeName Type { get; } = type;

    public Token? PointerSpecifier { get; } = pointerSpecifier;




    public override string ToString()
        => $"{Type}*";
}




public class ArrayTypeName(TypeName type, uint? size, Token leftBracket, Token rightBracket) : PointerTypeName(type)
{
    public uint? Size { get; } = size;

    public Token LeftBracket { get; } = leftBracket;
    public Token RightBracket { get; } = rightBracket;




    public override string ToString() => $"{Type}[{(Size is null ? "" : Size.Value.ToString())}]";
}




public class FunctionTypeName(TypeName returnType, IReadOnlyList<TypeName> parametersType) : PointerTypeName(returnType)
{
    public TypeName ReturnType => Type;
    public IReadOnlyList<TypeName> ParametersType { get; } = parametersType;




    public override string ToString()
    {
        var parameterTypesString = ParametersType.Select(parameter => parameter.ToString());
        var parameters = string.Join(", ", parameterTypesString);

        return $"{ReturnType}({parameters})";
    }
}
