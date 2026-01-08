using System.Collections.Generic;

using Torque.Compiler.AST.Statements;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundBlockStatement(BlockStatement syntax,  IReadOnlyList<BoundStatement> statements, Scope scope) : BoundStatement(syntax)
{
    public new BlockStatement Syntax => (base.Syntax as BlockStatement)!;

    public IReadOnlyList<BoundStatement> Statements { get; } = statements;

    public Scope Scope { get; } = scope;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessBlock(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessBlock(this);
}
