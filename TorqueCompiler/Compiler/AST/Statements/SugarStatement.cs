using System;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public abstract class SugarStatement(Span location) : Statement(location)
{
    public override void Process(IStatementProcessor processor) => throw new InvalidOperationException();
    public override T Process<T>(IStatementProcessor<T> processor) => throw new InvalidOperationException();


    public abstract Statement Process(ISugarStatementProcessor processor);
}
