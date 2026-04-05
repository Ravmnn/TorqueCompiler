using System.Collections.Generic;

using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;
using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.AST.Statements;




public class VariableDeclarationStatement(TypeSyntax type, SymbolSyntax name, Expression value, Span location)
    : Statement(location), IDeclaration
{
    public TypeSyntax Type { get; } = type;
    public SymbolSyntax Name { get; } = name;
    public Expression Value { get; set; } = value;
    public bool InferType { get; set; }

    public IList<Modifier> Modifiers { get; set; } = [];
    public ModifierTarget ThisTargetIdentity => ModifierTarget.LocalVariable;
    public SymbolSyntax Symbol => Name;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessVariableDefinition(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessVariableDefinition(this);


    public void ProcessDeclaration(IDeclarationProcessor processor)
    {}
}
