using System.Collections.Generic;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public class BlockStatement(IReadOnlyList<Statement> statements, Span location) : Statement(location)
{
    public IReadOnlyList<Statement> Statements { get; } = statements;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessBlock(this);
}
