using System;
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

        var identifier = new VariableSymbol(statement.Name, Scope);
        var value = Process(statement.Value);

        Scope.Symbols.Add(identifier);

        return new BoundDeclarationStatement(statement, identifier, value);
    }




    public BoundStatement ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        if (Scope.SymbolExists(statement.Name.Lexeme))
            ReportToken(Diagnostic.BinderCatalog.MultipleSymbolDeclaration, statement.Source());

        var symbol = new FunctionSymbol(statement.Name, Scope);
        Scope.Symbols.Add(symbol);

        var body = (ProcessFunctionBlock(statement.Body, statement.Parameters, out var parameterSymbols) as BoundBlockStatement)!;

        symbol.Parameters = parameterSymbols.ToArray();

        return new BoundFunctionDeclarationStatement(statement, body, symbol);
    }




    public BoundStatement ProcessReturn(ReturnStatement statement)
    {
        var expression = statement.Expression is not null ? Process(statement.Expression) : null;
        return new BoundReturnStatement(statement, expression);
    }




    public BoundStatement ProcessBlock(BlockStatement statement)
        => ProcessLexicalBlock(statement);


    private BoundStatement ProcessLexicalBlock(BlockStatement statement)
        => ProcessInnerScope(Scope, () => BlockToBound(statement));


    private BoundStatement ProcessFunctionBlock(BlockStatement statement, IEnumerable<FunctionParameterDeclaration> parameters,
        out IEnumerable<VariableSymbol> parameterSymbols)
    {
        var parameterSymbolsList = new List<VariableSymbol>();

        var boundBlock = ProcessInnerScope(Scope, () =>
        {
            foreach (var parameter in parameters)
                parameterSymbolsList.Add(new VariableSymbol(parameter.Name, Scope) { IsParameter = true });

            Scope.Symbols.AddRange(parameterSymbolsList);

            return BlockToBound(statement);
        });

        parameterSymbols = parameterSymbolsList;

        return boundBlock;
    }


    private BoundBlockStatement BlockToBound(BlockStatement statement)
    {
        var boundStatements = statement.Statements.Select(Process).ToArray();
        return new BoundBlockStatement(Scope, statement, boundStatements);
    }


    private T ProcessInnerScope<T>(Scope scope, Func<T> func)
    {
        var oldScope = Scope;
        Scope = new Scope(scope);

        var result = func();

        Scope = oldScope;

        return result;
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




    public BoundExpression ProcessUnary(UnaryExpression expression)
    {
        var boundExpression = Process(expression.Expression);

        return new BoundUnaryExpression(expression, boundExpression);
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

        else if (symbol is not VariableSymbol)
            ReportToken(Diagnostic.BinderCatalog.SymbolIsNotAValue, expression.Source());

        return new BoundSymbolExpression(expression, (symbol as VariableSymbol)!);
    }




    public BoundExpression ProcessAssignment(AssignmentExpression expression)
    {
        var pointer = Process(expression.Pointer);
        var reference = ToAssignmentReference(pointer);

        var value = Process(expression.Value);

        return new BoundAssignmentExpression(expression, reference, value);
    }


    private BoundAssignmentReferenceExpression ToAssignmentReference(BoundExpression expression)
    {
        if (expression is not BoundSymbolExpression and not BoundPointerAccessExpression)
            Report(Diagnostic.BinderCatalog.MustBeAssignmentReference, location: expression.Source());

        return new BoundAssignmentReferenceExpression(expression.Syntax, expression);
    }




    public BoundExpression ProcessPointerAccess(PointerAccessExpression expression)
        => new BoundPointerAccessExpression(expression, Process(expression.Pointer));




    public BoundExpression ProcessCall(CallExpression expression)
    {
        // TODO: check function arity (after add delegates)

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
