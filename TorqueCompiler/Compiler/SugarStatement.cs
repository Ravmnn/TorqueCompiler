using System;


namespace Torque.Compiler;




public abstract class SugarStatement(Span location) : Statement(location)
{
    public override void Process(IStatementProcessor processor) => throw new InvalidOperationException();
    public override T Process<T>(IStatementProcessor<T> processor) => throw new InvalidOperationException();


    public abstract Statement Process(ISugarStatementProcessor processor);
}




public class SugarDefaultDeclarationStatement(TypeName type, SymbolSyntax name) : SugarStatement(name.Location)
{
    public TypeName Type { get; } = type;
    public SymbolSyntax Name { get; } = name;




    public override Statement Process(ISugarStatementProcessor processor)
        => processor.ProcessDefaultDeclaration(this);
}
