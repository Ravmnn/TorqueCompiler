using Torque.Compiler.Types;


namespace Torque.Compiler.Semantic;




public class NamedTypeSyntaxBinder(DeclaredTypeManager declaredTypes) : ITypeSyntaxProcessor<TypeSyntax>
{
    public DeclaredTypeManager DeclaredTypes { get; } = declaredTypes;




    public TypeSyntax Process(TypeSyntax type)
        => type.Process(this);


    public TypeSyntax ProcessBase(BaseTypeSyntax type)
    {
        if (type.IsPrimitiveType)
            return type;

        // in case of unknown type, the binder will report
        var structTypeDeclaration = DeclaredTypes.TryGet<StructTypeDeclaration>(type.TypeSymbol.Name);
        return structTypeDeclaration?.TypeSyntax ?? type;
    }


    public TypeSyntax ProcessPointer(PointerTypeSyntax type)
    {
        type.InnerType = Process(type.InnerType);
        return type;
    }


    public TypeSyntax ProcessFunction(FunctionTypeSyntax type)
    {
        type.InnerType = Process(type.InnerType);

        for (var i = 0; i < type.ParametersType.Count; i++)
            type.ParametersType[i] = Process(type.ParametersType[i]);

        return type;
    }


    public TypeSyntax ProcessStruct(StructTypeSyntax type)
    {
        return type;
    }
}
