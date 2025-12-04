namespace Torque.Compiler;




public interface IBoundStatementProcessor
{
    void ProcessExpression(BoundExpressionStatement statement);
    void ProcessDeclaration(BoundDeclarationStatement statement);
    void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement);
    void ProcessReturn(BoundReturnStatement statement);
    void ProcessBlock(BoundBlockStatement statement);
}




public abstract class BoundStatement(Statement syntax)
{
    public Statement Syntax { get; } = syntax;


    public abstract void Process(IBoundStatementProcessor processor);
}




public class BoundExpressionStatement(ExpressionStatement syntax, BoundExpression expression) : BoundStatement(syntax)
{
    public BoundExpression Expression { get; } = expression;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessExpression(this);
}




public class BoundDeclarationStatement(DeclarationStatement syntax, IdentifierSymbol identifier) : BoundStatement(syntax)
{
    public IdentifierSymbol Identifier { get; } = identifier;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessDeclaration(this);
}




public class BoundFunctionDeclarationStatement(FunctionDeclarationStatement syntax) : BoundStatement(syntax)
{
    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessFunctionDeclaration(this);
}




public class BoundReturnStatement(ReturnStatement syntax, BoundExpression? expression) : BoundStatement(syntax)
{
    public BoundExpression? Expression { get; } = expression;


    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessReturn(this);
}




public class BoundBlockStatement(BlockStatement syntax) : BoundStatement(syntax)
{
    public override void Process(IBoundStatementProcessor processor)
        => processor.ProcessBlock(this);
}



