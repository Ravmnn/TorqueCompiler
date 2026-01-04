using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public class TorqueDesugarizer(IReadOnlyList<Statement> statements) : IStatementProcessor<Statement>, ISugarStatementProcessor
{
    public IReadOnlyList<Statement> Statements { get; } = statements;




    public IReadOnlyList<Statement> Desugarize()
        => Statements.Select(Process).ToArray();




    public Statement Process(Statement statement)
    {
        if (statement is not SugarStatement sugarStatement)
            return statement.Process(this);

        return sugarStatement.Process(this);
    }


    Statement IStatementProcessor<Statement>.Process(Statement statement)
        => statement.Process(this);




    public Statement ProcessExpression(ExpressionStatement statement)
        => statement;


    public Statement ProcessDeclaration(DeclarationStatement statement)
        => statement;


    public Statement ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
        => new FunctionDeclarationStatement(statement.ReturnType, statement.Name, statement.Parameters,
            (Process(statement.Body) as BlockStatement)!);


    public Statement ProcessReturn(ReturnStatement statement)
        => statement;


    public Statement ProcessBlock(BlockStatement statement)
    {
        var desugarizedStatements = statement.Statements.Select(Process).ToArray();
        return new BlockStatement(statement.Start, statement.End, desugarizedStatements);
    }




    public Statement ProcessDefaultDeclaration(SugarDefaultDeclarationStatement statement)
    {
        var defaultValue = new DefaultExpression(default, default, statement.Type, default);
        return new DeclarationStatement(statement.Type, statement.Name, defaultValue);
    }
}
