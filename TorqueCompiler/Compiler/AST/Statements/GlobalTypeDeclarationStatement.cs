using System;
using System.Collections.Generic;

using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.Compiler.AST.Statements;




public abstract class GlobalTypeDeclarationStatement(SymbolSyntax symbol, Span location)
    : Statement(location), IDeclaration
{
    public IReadOnlyList<Modifier> Modifiers { get; set; } = [];
    public abstract ModifierTarget ThisTargetIdentity { get; }
    public SymbolSyntax Symbol { get; } = symbol;

    public bool CanBeInFileScope => true;
    public bool CanBeInFunctionScope => false;




    public override void Process(IStatementProcessor processor) {}
    public override T Process<T>(IStatementProcessor<T> processor) => default!;


    private static Exception CannotCallThisMethod()
        => new InvalidOperationException("Cannot call the statement processor method of a \"GlobalTypeDeclaration\"");




    public abstract void ProcessDeclaration(IDeclarationProcessor processor);

    public abstract T ProcessGlobalTypeDeclaration<T>(IGlobalTypeDeclarationProcessor<T> processor);




    public abstract TypeDeclaration GetTypeDeclaration();
}
