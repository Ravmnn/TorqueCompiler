using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




// TODO: should use DeclarationStatement, for struct fields as well
public readonly record struct FunctionParameterDeclaration(SymbolSyntax Name, TypeSyntax Type);


public class FunctionDeclarationStatement(TypeSyntax returnType, SymbolSyntax name, IReadOnlyList<FunctionParameterDeclaration> parameters,
    BlockStatement? body) : Statement(name.Location), IModificable
{
    public TypeSyntax ReturnType { get; } = returnType;
    public SymbolSyntax Name { get; } = name;
    public IReadOnlyList<FunctionParameterDeclaration> Parameters { get; } = parameters;
    public BlockStatement? Body { get; set; } = body;

    public IReadOnlyList<Modifier> Modifiers { get; set; } = [];
    public ModifierTarget ThisTargetIdentity => ModifierTarget.Function;

    public bool IsExternal => Modifiers.Any(modifier => modifier.Type == TokenType.KwExternal);




    public override void Process(IStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);
}
