using System;


namespace Torque.Compiler;




public abstract class SugarStatement : Statement
{
    public override void Process(IStatementProcessor processor) => throw new InvalidOperationException();
    public override T Process<T>(IStatementProcessor<T> processor) => throw new InvalidOperationException();


    public abstract Statement Process(ISugarStatementProcessor processor);
}




public class SugarDefaultDeclarationStatement(TypeName type, Token name) : SugarStatement
{
    public TypeName Type { get; } = type;
    public Token Name { get; } = name;




    public override Statement Process(ISugarStatementProcessor processor)
        => processor.ProcessDefaultDeclaration(this);


    public override Token Source() => Name;
    public override SourceLocation Location()
        => new SourceLocation(Type.Base.TypeToken.Location, Name);
}
