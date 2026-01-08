using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundDeclarationStatement(DeclarationStatement syntax, VariableSymbol symbol, BoundExpression value) : BoundStatement(syntax)
{
    public new DeclarationStatement Syntax => (base.Syntax as DeclarationStatement)!;

    public BoundExpression Value { get; set; } = value;

    public VariableSymbol Symbol { get; } = symbol;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);
}
