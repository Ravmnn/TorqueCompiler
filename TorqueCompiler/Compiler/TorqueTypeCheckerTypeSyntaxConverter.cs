using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Diagnostics.Catalogs;
using Torque.Compiler.Types;


namespace Torque.Compiler;




public class TorqueTypeCheckerTypeSyntaxConverter(TorqueTypeChecker typeChecker)
{
    private readonly List<StructType> _processedStructs = [];
    private bool _insideAPointer;


    public TorqueTypeChecker TypeChecker { get; } = typeChecker;




    public Type TypeFromNonVoidTypeSyntax(TypeSyntax typeSyntax)
    {
        var type = TypeFromTypeSyntax(typeSyntax);
        TypeChecker.Reporter.ReportIfVoidTypeName(type, typeSyntax.BaseType.TypeSymbol.Location);

        return type;
    }


    public Type TypeFromTypeSyntax(TypeSyntax typeSyntax)
    {
        _processedStructs.Clear();
        return TypeFromTypeSyntaxInternal(typeSyntax);
    }




    private Type TypeFromTypeSyntaxInternal(TypeSyntax typeSyntax) => typeSyntax switch
    {
        StructTypeSyntax structTypeSyntax => StructTypeFromTypeSyntax(structTypeSyntax),

        FunctionTypeSyntax functionTypeName => FunctionTypeFromTypeSyntax(functionTypeName),
        PointerTypeSyntax pointerTypeName => PointerTypeFromTypeSyntax(pointerTypeName),

        BaseTypeSyntax baseTypeName => BaseTypeFromTypeSyntax(baseTypeName),

        _ => throw new UnreachableException()
    };




    private StructType StructTypeFromTypeSyntax(StructTypeSyntax structTypeSyntax)
    {
        var structType = new StructType(structTypeSyntax.Name, []);
        var structCache = _processedStructs.FirstOrDefault(structs => structs.Name.Name == structTypeSyntax.Name.Name);

        if (structCache is not null)
        {
            if (_insideAPointer)
                return structCache;

            TypeChecker.Reporter.Report(TypeCheckerCatalog.InfiniteTypeRecursionChain, location: structTypeSyntax.Name.Location);
            return structType;
        }

        _processedStructs.Add(structType);

        structType.Members = BindStructGenericDeclarationMembers(structTypeSyntax);

        return structType;
    }


    private List<BoundGenericDeclaration> BindStructGenericDeclarationMembers(StructTypeSyntax structTypeSyntax)
    {
        var boundMembers = new List<BoundGenericDeclaration>();

        foreach (var member in structTypeSyntax.Members)
            boundMembers.Add(new BoundGenericDeclaration(TypeFromTypeSyntaxInternal(member.Type), member.Name));

        return boundMembers;
    }


    private FunctionType FunctionTypeFromTypeSyntax(FunctionTypeSyntax typeSyntax)
    {
        _insideAPointer = true;
        var parametersType = typeSyntax.ParametersType.Select(TypeFromTypeSyntaxInternal).ToArray();
        var returnType = TypeFromTypeSyntaxInternal(typeSyntax.ReturnType);
        _insideAPointer = false;

        return new FunctionType(returnType, parametersType);
    }




    private PointerType PointerTypeFromTypeSyntax(PointerTypeSyntax pointerTypeSyntax)
    {
        _insideAPointer = true;
        var pointer = new PointerType(TypeFromTypeSyntaxInternal(pointerTypeSyntax.InnerType));
        _insideAPointer = false;

        return pointer;
    }


    private Type BaseTypeFromTypeSyntax(BaseTypeSyntax typeSyntax)
    {
        if (typeSyntax.IsPrimitiveType)
            return new BasePrimitiveType(typeSyntax.BaseType.TypeSymbol.SymbolToPrimitiveType());

        return TypeFromDeclaredTypes(typeSyntax);
    }




    private Type TypeFromDeclaredTypes(BaseTypeSyntax typeSyntax)
    {
        var typeDeclaration = TypeChecker.DeclaredTypes.TryGetType(typeSyntax.TypeSymbol.Name)!;
        var declarationTypeSyntax = typeDeclaration.GetTypeSyntax();
        return TypeFromTypeSyntaxInternal(declarationTypeSyntax);
    }
}
