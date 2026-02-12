using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;
using Torque.Compiler.Types;


namespace Torque.Compiler.AST.Statements;




public class AliasDeclarationStatement(SymbolSyntax name, TypeSyntax typeSyntax, Span location)
    : GlobalTypeDeclarationStatement(name, location)
{
    public TypeSyntax TypeSyntax { get; } = typeSyntax;

    public override ModifierTarget ThisTargetIdentity => ModifierTarget.Alias;




    public override void ProcessDeclaration(IDeclarationProcessor processor)
        => processor.ProcessAliasDeclaration(this);


    public override T ProcessGlobalTypeDeclaration<T>(IGlobalTypeDeclarationProcessor<T> processor)
        => processor.ProcessAlias(this);




    public override AliasTypeDeclaration GetTypeDeclaration()
        => new AliasTypeDeclaration(Symbol, TypeSyntax);
}
