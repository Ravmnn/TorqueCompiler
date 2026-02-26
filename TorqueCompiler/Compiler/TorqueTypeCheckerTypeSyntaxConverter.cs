using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Types;


namespace Torque.Compiler;




public class TorqueTypeCheckerTypeSyntaxConverter(TorqueTypeChecker typeChecker)
{
    public TorqueTypeChecker TypeChecker { get; } = typeChecker;




    public Type TypeFromNonVoidTypeSyntax(TypeSyntax typeSyntax)
    {
        var type = TypeFromTypeSyntax(typeSyntax);
        TypeChecker.Reporter.ReportIfVoidTypeName(type, typeSyntax.BaseType.TypeSymbol.Location);

        return type;
    }




    public Type TypeFromTypeSyntax(TypeSyntax typeSyntax) => typeSyntax switch
    {
        StructTypeSyntax structTypeSyntax => StructTypeFromTypeSyntax(structTypeSyntax),

        FunctionTypeSyntax functionTypeName => FunctionTypeFromTypeSyntax(functionTypeName),
        PointerTypeSyntax pointerTypeName => TypeFromPointerTypeSyntax(pointerTypeName),

        BaseTypeSyntax baseTypeName => TypeFromBaseTypeSyntax(baseTypeName),

        _ => throw new UnreachableException()
    };


    // TODO: "variable as Person*" throws an exception
    // TODO: add "->" operator for pointers
    // TODO: structs should pass their copy
    public StructType StructTypeFromTypeSyntax(StructTypeSyntax structTypeSyntax)
    {
        // TODO: allow struct type recursion (the same struct inside the struct)

        var boundMembers = new List<BoundGenericDeclaration>();

        foreach (var member in structTypeSyntax.Members)
            boundMembers.Add(new BoundGenericDeclaration(TypeFromTypeSyntax(member.Type), member.Name));

        return new StructType(structTypeSyntax.SymbolSyntax, boundMembers);
    }


    public FunctionType FunctionTypeFromTypeSyntax(FunctionTypeSyntax typeSyntax)
    {
        var parametersType = typeSyntax.ParametersType.Select(TypeFromTypeSyntax).ToArray();
        var returnType = TypeFromTypeSyntax(typeSyntax.ReturnType);

        return new FunctionType(returnType, parametersType);
    }


    public PointerType TypeFromPointerTypeSyntax(PointerTypeSyntax pointerTypeSyntax)
        => new PointerType(TypeFromTypeSyntax(pointerTypeSyntax.InnerType));


    public Type TypeFromBaseTypeSyntax(BaseTypeSyntax typeSyntax)
    {
        if (typeSyntax.IsPrimitiveType)
            return new BasePrimitiveType(typeSyntax.BaseType.TypeSymbol.SymbolToPrimitiveType());

        return TypeFromDeclaredTypes(typeSyntax);
    }


    public Type TypeFromDeclaredTypes(BaseTypeSyntax typeSyntax)
    {
        var typeDeclaration = TypeChecker.DeclaredTypes.TryGetType(typeSyntax.TypeSymbol.Name)!;
        var declarationTypeSyntax = typeDeclaration.GetTypeSyntax();
        return TypeFromTypeSyntax(declarationTypeSyntax);
    }
}
