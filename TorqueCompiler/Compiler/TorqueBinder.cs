using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueBinder(IEnumerable<Statement> statements) : DiagnosticReporter<Diagnostic.SymbolResolverCatalog>,
    IExpressionProcessor<BoundExpression>, IStatementProcessor<BoundStatement>
{
    private Scope _scope = new Scope();


    public IEnumerable<Statement> Statements { get; } = statements;




    public IEnumerable<BoundStatement> Bind()
    {
        Diagnostics.Clear();

        return Statements.Select(Process).ToArray();
    }




    private BoundStatement Process(Statement statement)
        => statement.Process(this);


    public BoundStatement ProcessExpression(ExpressionStatement statement)
    {
        return new BoundExpressionStatement(statement, Process(statement.Expression));
    }




    public BoundStatement ProcessDeclaration(DeclarationStatement statement)
    {
        if (_scope.SymbolExists(statement.Name.Lexeme))
            ReportToken(Diagnostic.SymbolResolverCatalog.MultipleSymbolDeclaration, statement.Source());

        var identifier = new ValueSymbol(statement.Name.Lexeme, null, statement.Name.Location, _scope);
        var value = Process(statement.Value);

        _scope.Symbols.Add(identifier);

        return new BoundDeclarationStatement(statement, identifier, value);
    }




    public BoundStatement ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        if (_scope.SymbolExists(statement.Name.Lexeme))
            ReportToken(Diagnostic.SymbolResolverCatalog.MultipleSymbolDeclaration, statement.Source());

        var symbol = new FunctionSymbol(statement.Name.Lexeme, null, null, statement.Name.Location, _scope);
        _scope.Symbols.Add(symbol);

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
        _scope = new Scope(_scope);

        var boundStatements = statement.Statements.Select(Process).ToArray();

        _scope = _scope.Parent!;

        return new BoundBlockStatement(statement, boundStatements);
    }








    private BoundExpression Process(Expression expression)
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




    public BoundExpression ProcessIdentifier(IdentifierExpression expression)
    {
        var symbol = _scope.TryGetSymbol(expression.Identifier.Lexeme);

        if (symbol is null)
            ReportToken(Diagnostic.SymbolResolverCatalog.UndeclaredSymbol, expression.Source());

        else if (symbol is not ValueSymbol)
            ReportToken(Diagnostic.SymbolResolverCatalog.SymbolIsNotAValue, expression.Source());

        return new BoundIdentifierExpression(expression, (symbol as ValueSymbol)!);
    }




    public BoundExpression ProcessAssignment(AssignmentExpression expression)
    {
        var identifier = (Process(expression.Identifier) as BoundIdentifierExpression)!;
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
