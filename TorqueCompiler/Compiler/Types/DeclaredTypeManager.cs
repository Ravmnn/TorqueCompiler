namespace Torque.Compiler.Types;




public class DeclaredTypeManager : ImportableStorage<TypeDeclaration>
{
    public bool IsTypeDeclarationSyntaxOfType<T>(string symbol) where T : TypeSyntax
        => TryGet(symbol)?.TypeSyntax is T;
}
