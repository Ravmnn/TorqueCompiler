using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler.Types;




public class DeclaredTypeManager
{
    public List<TypeDeclaration> Types { get; set; } = [];




    public T? TryGetType<T>(string symbol) where T : TypeDeclaration
        => TryGetType(symbol) as T;


    public TypeDeclaration? TryGetType(string symbol)
        => Types.FirstOrDefault(declaredType => declaredType.TypeSymbol.Name == symbol);




    public bool IsTypeDeclarationSyntaxOfType<T>(string symbol) where T : TypeSyntax
        => TryGetType(symbol)?.GetTypeSyntax() is T;


    public bool IsDeclared<T>(string symbol) where T : TypeDeclaration
        => TryGetType<T>(symbol) is not null;


    public bool IsDeclared(string symbol)
        => TryGetType(symbol) is not null;




    public bool TypeIsMultiDeclared(string symbol)
        => Types.Count(type => type.TypeSymbol.Name == symbol) > 1;
}
