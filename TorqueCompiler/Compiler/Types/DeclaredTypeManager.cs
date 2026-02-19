using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Types;


namespace Torque.Compiler.Types;




public class DeclaredTypeManager
{
    public List<TypeDeclaration> Types { get; set; } = [];




    public T? TryGetType<T>(string symbol) where T : TypeDeclaration
        => TryGetType(symbol) as T;


    public TypeDeclaration? TryGetType(string symbol)
        => Types.FirstOrDefault(declaredType => declaredType.TypeSymbol.Name == symbol);




    public bool IsDeclared<T>(string symbol) where T : TypeDeclaration
        => TryGetType<T>(symbol) is not null;


    public bool IsDeclared(string symbol)
        => TryGetType(symbol) is not null;
}
