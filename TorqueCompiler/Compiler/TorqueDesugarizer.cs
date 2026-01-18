using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;


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
    {
        var desugarizedBody = statement.Body is not null ? Process(statement.Body) : null;
        statement.Body = (desugarizedBody as BlockStatement)!;

        return statement;
    }


    public Statement ProcessReturn(ReturnStatement statement)
        => statement;




    public Statement ProcessBlock(BlockStatement statement)
    {
        var desugarizedStatements = statement.Statements.Select(Process).ToArray();
        statement.Statements = desugarizedStatements;

        return statement;
    }




    public Statement ProcessIf(IfStatement statement)
    {
        var desugarizedThenStatement = Process(statement.ThenStatement);
        var desugarizedElseStatement = statement.ElseStatement is not null ? Process(statement.ElseStatement) : null;

        statement.ThenStatement = desugarizedThenStatement;
        statement.ElseStatement = desugarizedElseStatement;

        return statement;
    }








    public Statement ProcessDefaultDeclaration(SugarDefaultDeclarationStatement statement)
    {
        var defaultValue = new DefaultExpression(statement.Type, statement.Location);
        return new DeclarationStatement(statement.Type, statement.Name, defaultValue) { Modifiers = statement.Modifiers };
    }
}
