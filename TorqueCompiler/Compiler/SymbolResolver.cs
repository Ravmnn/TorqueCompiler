using System;
using System.Collections.Generic;
using System.Linq;
using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class SymbolResolver : DiagnosticReporter<Diagnostic.SymbolResolverCatalog>,
    IExpressionProcessor<BoundExpression>, IStatementProcessor<BoundStatement>
{
    private readonly List<BoundStatement> _statements = [];
    private Scope _scope = new Scope();




    public IEnumerable<BoundStatement> Resolve(IEnumerable<Statement> statements)
    {
        Diagnostics.Clear();
        _statements.Clear();

        foreach (var statement in statements)
            _statements.Add(Process(statement));

        return _statements;
    }




    public BoundStatement Process(Statement statement)
        => statement.Process(this);




    public BoundStatement ProcessExpression(ExpressionStatement statement)
    {
        return new BoundExpressionStatement(statement, Process(statement.Expression));
    }




    public BoundStatement ProcessDeclaration(DeclarationStatement statement)
    {
        if (_scope.SymbolExists(statement.Name.Lexeme))
            Report(Diagnostic.SymbolResolverCatalog.MultipleIdentifierDeclaration);

        var identifier = new IdentifierSymbol(statement.Name.Lexeme, null, statement.Name.Location, _scope);
        _scope.Symbols.Add(identifier);

        return new BoundDeclarationStatement(statement, identifier);
    }




    public BoundStatement ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        if (_scope.SymbolExists(statement.Name.Lexeme))
            ReportAndThrow(Diagnostic.SymbolResolverCatalog.MultipleIdentifierDeclaration);

        _scope.Symbols.Add(new FunctionSymbol(statement.Name.Lexeme, null, null, statement.Name.Location, _scope));

        return new BoundFunctionDeclarationStatement(statement);
    }




    public BoundStatement ProcessReturn(ReturnStatement statement)
    {
        var expression = statement.Expression is not null ? Process(statement.Expression) : null;
        return new BoundReturnStatement(statement, expression);
    }




    public BoundStatement ProcessBlock(BlockStatement statement)
    {
        _scope = new Scope(_scope);

        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);

        _scope = _scope.Parent!;

        return new BoundBlockStatement(statement);
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




    public BoundExpression ProcessIdentifier(IdentifierExpression expression)
    {
        if (!_scope.SymbolExists(expression.Identifier.Lexeme))
            Report(Diagnostic.SymbolResolverCatalog.UndeclaredIdentifier);

        if (_scope.GetSymbol(expression.Identifier.Lexeme) is not IdentifierSymbol identifier)
            throw new InvalidOperationException("Is not identifier symbol");

        return new BoundIdentifierExpression(expression, identifier);
    }




    public BoundExpression ProcessAssignment(AssignmentExpression expression)
    {
        var identifier = (Process(expression.Identifier) as BoundIdentifierExpression)!;

        return new BoundAssignmentExpression(expression, identifier);
    }




    public BoundExpression ProcessCall(CallExpression expression)
    {
        var callee = Process(expression.Callee);

        return new BoundCallExpression(expression, callee);
    }




    public BoundExpression ProcessCast(CastExpression expression)
    {
        return new BoundCastExpression(expression);
    }
}
