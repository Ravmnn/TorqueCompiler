using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueBinder(IEnumerable<Statement> statements) : DiagnosticReporter<Diagnostic.BinderCatalog>,
    IExpressionProcessor<BoundExpression>, IStatementProcessor<BoundStatement>
{
    public IEnumerable<Statement> Statements { get; } = statements;

    public Scope Scope { get; private set; } = new Scope();




    public IEnumerable<BoundStatement> Bind()
    {
        Diagnostics.Clear();

        return Statements.Select(Process).ToArray();
    }




    public BoundStatement Process(Statement statement)
        => statement.Process(this);


    public BoundStatement ProcessExpression(ExpressionStatement statement)
    {
        return new BoundExpressionStatement(statement, Process(statement.Expression));
    }




    public BoundStatement ProcessDeclaration(DeclarationStatement statement)
    {
        if (Scope.SymbolExists(statement.Name.Lexeme))
            ReportToken(Diagnostic.BinderCatalog.MultipleSymbolDeclaration, statement.Source());

        var identifier = new ValueSymbol(statement.Name.Lexeme, null, statement.Name.Location, Scope);
        var value = Process(statement.Value);

        Scope.Symbols.Add(identifier);

        return new BoundDeclarationStatement(statement, identifier, value);
    }




    public BoundStatement ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        if (Scope.SymbolExists(statement.Name.Lexeme))
            ReportToken(Diagnostic.BinderCatalog.MultipleSymbolDeclaration, statement.Source());

        var symbol = new FunctionSymbol(statement.Name.Lexeme, null, null, statement.Name.Location, Scope);
        Scope.Symbols.Add(symbol);

        var body = (Process(statement.Body) as BoundBlockStatement)!;

        return new BoundFunctionDeclarationStatement(statement, body, symbol);
    }




    public BoundStatement ProcessReturn(ReturnStatement statement)
    {
        var expression = statement.Expression is not null ? Process(statement.Expression) : null;
        return new BoundReturnStatement(statement, expression);
    }




    public BoundStatement ProcessBlock(BlockStatement statement)
    {
        Scope = new Scope(Scope);

        var boundStatements = statement.Statements.Select(Process).ToArray();
        var blockStatement = new BoundBlockStatement(Scope, statement, boundStatements);

        Scope = Scope.Parent!;

        return blockStatement;
    }








    public BoundExpression Process(Expression expression)
        => expression.Process(this);




    public BoundExpression ProcessLiteral(LiteralExpression expression)
    {
        return new BoundLiteralExpression(expression);
    }




    public BoundExpression ProcessBinary(BinaryExpression expression)
    {
        var left = Process(expression.Left);
        var right = Process(expression.Right);

        return new BoundBinaryExpression(expression, left, right);
    }




    public BoundExpression ProcessGrouping(GroupingExpression expression)
    {
        var boundExpression = Process(expression.Expression);

        return new BoundGroupingExpression(expression, boundExpression);
    }




    public BoundExpression ProcessSymbol(SymbolExpression expression)
    {
        var symbol = Scope.TryGetSymbol(expression.Identifier.Lexeme);

        if (symbol is null)
            ReportToken(Diagnostic.BinderCatalog.UndeclaredSymbol, expression.Source());

        else if (symbol is not ValueSymbol)
            ReportToken(Diagnostic.BinderCatalog.SymbolIsNotAValue, expression.Source());

        return new BoundSymbolExpression(expression, (symbol as ValueSymbol)!);
    }




    public BoundExpression ProcessAssignment(AssignmentExpression expression)
    {
        var identifier = (Process(expression.Symbol) as BoundSymbolExpression)!;
        var value = Process(expression.Value);

        return new BoundAssignmentExpression(expression, identifier, value);
    }




    public BoundExpression ProcessCall(CallExpression expression)
    {
        var callee = Process(expression.Callee);
        var arguments = expression.Arguments.Select(Process).ToArray();

        return new BoundCallExpression(expression, callee, arguments);
    }




    public BoundExpression ProcessCast(CastExpression expression)
    {
        var value = Process(expression.Expression);
        return new BoundCastExpression(expression, value);
    }
}
