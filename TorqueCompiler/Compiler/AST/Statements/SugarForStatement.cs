using Torque.Compiler.Tokens;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.AST.Statements;




public class SugarForStatement(Statement initialization, Expression condition, Expression step, Statement body, Span location)
    : SugarStatement(location)
{
    public Statement Initialization { get; set; } = initialization;
    public Expression Condition { get; set; } = condition;
    public Expression Step { get; set; } = step;
    public Statement Loop { get; set; } = body;




    public override Statement Process(ISugarStatementProcessor processor)
        => processor.ProcessFor(this);
}
