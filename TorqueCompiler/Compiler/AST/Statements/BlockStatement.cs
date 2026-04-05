using System.Collections.Generic;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public class BlockStatement(IReadOnlyCollection<Statement> statements, Span location) : Statement(location)
{
    public IReadOnlyCollection<Statement> Statements { get; set; } = statements;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessBlock(this);
}
