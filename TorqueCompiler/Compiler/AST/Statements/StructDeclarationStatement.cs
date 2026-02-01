using System.Collections.Generic;
using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;


namespace Torque.Compiler.AST.Statements;




public class StructDeclarationStatement(SymbolSyntax symbol, IReadOnlyList<GenericDeclaration> members, Span location)
    : GlobalTypeDeclaration(symbol, location)
{
    public override ModifierTarget ThisTargetIdentity => ModifierTarget.Struct;

    public IReadOnlyList<GenericDeclaration> Members { get; } = members;




    public override void ProcessDeclaration(IDeclarationProcessor processor)
        => processor.ProcessStructDeclaration(this);


    public override T ProcessGlobalTypeDeclaration<T>(IGlobalTypeDeclarationProcessor<T> processor)
        => processor.ProcessStruct(this);
}
