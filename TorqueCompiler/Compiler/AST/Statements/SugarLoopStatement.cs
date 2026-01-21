using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public class SugarLoopStatement(Statement body, Span location) : SugarStatement(location)
{
    public Statement Body { get; set; } = body;




    public override Statement Process(ISugarStatementProcessor processor)
        => processor.ProcessLoop(this);
}
