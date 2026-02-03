using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundVariableDeclarationStatement(VariableDeclarationStatement syntax, VariableSymbol variableSymbol, BoundExpression value)
    : BoundStatement(syntax), IBoundDeclaration
{
    public new VariableDeclarationStatement Syntax => (base.Syntax as VariableDeclarationStatement)!;

    public BoundExpression Value { get; set; } = value;
    public bool InferType => Syntax.InferType;

    public VariableSymbol VariableSymbol { get; } = variableSymbol;
    public Symbol Symbol => VariableSymbol;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessVariable(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);


    public void ProcessDeclaration(IBoundDeclarationProcessor processor)
    {}
}
