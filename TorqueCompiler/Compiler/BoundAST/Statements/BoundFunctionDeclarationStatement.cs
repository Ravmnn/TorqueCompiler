using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Statements;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundFunctionDeclarationStatement(FunctionDeclarationStatement syntax, BoundBlockStatement body, FunctionSymbol symbol)
    : BoundStatement(syntax)
{
    public new FunctionDeclarationStatement Syntax => (base.Syntax as FunctionDeclarationStatement)!;

    public BoundBlockStatement Body { get; } = body;

    public FunctionSymbol Symbol { get; } = symbol;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);
}
