using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.Symbols;


namespace Torque.Compiler.AST.Statements;




public class FunctionDeclarationStatement(TypeSyntax returnType, SymbolSyntax name, IReadOnlyList<GenericDeclaration> parameters,
    BlockStatement? body) : Statement(name.Location), IDeclaration
{
    public TypeSyntax ReturnType { get; } = returnType;
    public SymbolSyntax Name { get; } = name;
    public IReadOnlyList<GenericDeclaration> Parameters { get; } = parameters;
    public BlockStatement? Body { get; set; } = body;

    public IList<Modifier> Modifiers { get; set; } = [];
    public ModifierTarget ThisTargetIdentity => ModifierTarget.Function;
    public SymbolSyntax Symbol => Name;

    public override bool CanBeInFileScope => true;
    public override bool CanBeInFunctionScope => false;

    public bool IsExternal => Modifiers.Any(modifier => modifier.Type == TokenType.KwExternal);




    public override void Process(IStatementProcessor processor)
        => processor.ProcessFunctionDefinition(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessFunctionDefinition(this);


    public void ProcessDeclaration(IDeclarationProcessor processor)
        => processor.ProcessFunctionDeclaration(this);
}
