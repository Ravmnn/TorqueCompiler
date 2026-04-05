using System.Collections.Generic;

using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public class SugarDefaultDeclarationStatement(TypeSyntax type, SymbolSyntax name)
    : SugarStatement(name.Location), IModificable
{
    public TypeSyntax Type { get; } = type;
    public SymbolSyntax Name { get; } = name;

    public IList<Modifier> Modifiers { get; set; } = [];
    public ModifierTarget ThisTargetIdentity => ModifierTarget.LocalVariable;




    public override Statement Process(ISugarStatementProcessor processor)
        => processor.ProcessDefaultDeclaration(this);

}
