using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Statements;


namespace Torque.Compiler.BoundAST.Statements;




public class BoundFunctionDeclarationStatement(FunctionDeclarationStatement syntax, BoundBlockStatement? body, FunctionSymbol functionSymbol)
    : BoundStatement(syntax), IBoundDeclaration
{
    public new FunctionDeclarationStatement Syntax => (base.Syntax as FunctionDeclarationStatement)!;

    public BoundBlockStatement? Body { get; } = body;
    public bool IsExternal => syntax.IsExternal;

    public FunctionSymbol FunctionSymbol { get; } = functionSymbol;
    public Symbol Symbol => FunctionSymbol;




    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessFunction(this);


    public override T Process<T>(IBoundStatementProcessor<T> processor)
        => processor.ProcessFunctionDeclaration(this);


    public void ProcessDeclaration(IBoundDeclarationProcessor processor)
        => processor.ProcessFunctionDeclaration(this);
}
