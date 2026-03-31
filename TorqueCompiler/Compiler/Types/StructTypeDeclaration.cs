using System.Collections.Generic;

using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class StructTypeDeclaration(SymbolSyntax typeSymbol, IReadOnlyList<GenericDeclaration> members)
    : TypeDeclaration(typeSymbol), ICompiledImportable
{
    public override TypeSyntax TypeSyntax { get; set; } = new StructTypeSyntax(typeSymbol, members);
    public override Type? Type { get; set; }




    public bool CanBeCompiled => true;

    public void Process(IImportableProcessor processor)
        => processor.ProcessStructImport(this);
}
