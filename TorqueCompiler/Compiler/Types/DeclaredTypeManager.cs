using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler.Types;




public class DeclaredTypeManager
{
    public List<TypeDeclaration> Types { get; set; } = [];
    public List<DeclaredTypeManager> ImportedTypeManagers { get; set; } = [];




    public T? TryGetType<T>(string symbol) where T : TypeDeclaration
        => TryGetType(symbol) as T;


    // TODO: both the scope and the type manager have similiarities, maybe you could add abstractions?
    public TypeDeclaration? TryGetType(string symbol)
    {
        var inThisManager = Types.FirstOrDefault(declaredType => declaredType.TypeSymbol.Name == symbol);

        if (inThisManager is not null)
            return inThisManager;

        return TryGetTypeFromImportedManagers(symbol);
    }

    private TypeDeclaration? TryGetTypeFromImportedManagers(string symbol)
    {
        foreach (var importedManager in ImportedTypeManagers)
            if (importedManager.TryGetType(symbol) is { } type)
                return type;

        return null;
    }




    public bool IsTypeDeclarationSyntaxOfType<T>(string symbol) where T : TypeSyntax
        => TryGetType(symbol)?.TypeSyntax is T;


    public bool IsDeclared<T>(string symbol) where T : TypeDeclaration
        => TryGetType<T>(symbol) is not null;


    public bool IsDeclared(string symbol)
        => TryGetType(symbol) is not null;




    public bool TypeIsMultiDeclared(string symbol)
        => Types.Count(type => type.TypeSymbol.Name == symbol) > 1;
}
