using System.Collections.Generic;

using Torque.Compiler.Types;
using Torque.Compiler.Symbols;


namespace Torque.Compiler.AST.Statements;




public readonly record struct FunctionParameterDeclaration(SymbolSyntax Name, TypeSyntax Type);


public class FunctionDeclarationStatement(TypeSyntax returnType, SymbolSyntax name, IReadOnlyList<FunctionParameterDeclaration> parameters,
    BlockStatement body) : Statement(name.Location)
{
    public TypeSyntax ReturnType { get; } = returnType;
    public SymbolSyntax Name { get; } = name;
    public IReadOnlyList<FunctionParameterDeclaration> Parameters { get; } = parameters;
    public BlockStatement Body { get; } = body;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);
}
