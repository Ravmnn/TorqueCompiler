using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Symbols;
using Torque.Compiler.Types;


namespace Torque.Compiler;




public class DeclaredTypeManager
{
    public List<TypeDeclaration> Types { get; set; } = [];




    public T? TryGetType<T>(SymbolSyntax symbol) where T : TypeDeclaration
        => TryGetType(symbol) as T;


    public TypeDeclaration? TryGetType(SymbolSyntax symbol)
        => Types.FirstOrDefault(declaredType => declaredType.TypeSymbol.Name == symbol.Name);




    public bool IsDeclared<T>(SymbolSyntax symbol) where T : TypeDeclaration
        => TryGetType<T>(symbol) is not null;


    public bool IsDeclared(SymbolSyntax symbol)
        => TryGetType(symbol) is not null;
}
