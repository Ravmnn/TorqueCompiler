using System;
using System.Collections.Generic;

using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;


// TODO: global type declarations must be in global (file) scope

public abstract class GlobalTypeDeclaration(SymbolSyntax symbol, Span location)
    : Statement(location), IDeclaration
{
    public IReadOnlyList<Modifier> Modifiers { get; set; } = [];
    public abstract ModifierTarget ThisTargetIdentity { get; }
    public SymbolSyntax Symbol { get; } = symbol;




    public override void Process(IStatementProcessor processor)
        => throw CannotCallThisMethod();


    public override T Process<T>(IStatementProcessor<T> processor)
        => throw CannotCallThisMethod();


    private static Exception CannotCallThisMethod()
        => new InvalidOperationException("Cannot call the statement processor method of a \"GlobalTypeDeclaration\"");




    public abstract void ProcessDeclaration(IDeclarationProcessor processor);

    public abstract T ProcessGlobalTypeDeclaration<T>(IGlobalTypeDeclarationProcessor<T> processor);
}
