using System;
using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueBinder(IEnumerable<Statement> statements) : DiagnosticReporter<Diagnostic.BinderCatalog>,
    IExpressionProcessor<BoundExpression>, IStatementProcessor<BoundStatement>
{
    public IEnumerable<Statement> Statements { get; } = statements;


    private Scope _scope = new Scope();
    public Scope Scope
    {
        get => _scope;
        private set => _scope = value;
    }


    public IEnumerable<BoundStatement> Bind()
    {
        Diagnostics.Clear();

        return Statements.Select(Process).ToArray();
    }




    #region Statements

    public BoundStatement Process(Statement statement)
        => statement.Process(this);


    public IEnumerable<BoundStatement> ProcessAll(IEnumerable<Statement> statements)
        => statements.Select(Process).ToArray();




    public BoundStatement ProcessExpression(ExpressionStatement statement)
        => new BoundExpressionStatement(statement, Process(statement.Expression));




    public BoundStatement ProcessDeclaration(DeclarationStatement statement)
    {
        ReportIfMultipleDeclaration(statement.Name);

        var identifier = new VariableSymbol(statement.Name, Scope);
        var value = Process(statement.Value);

        Scope.Symbols.Add(identifier);

        return new BoundDeclarationStatement(statement, identifier, value);
    }




    public BoundStatement ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        ReportIfMultipleDeclaration(statement.Name);

        var functionSymbol = new FunctionSymbol(statement.Name, Scope);
        Scope.Symbols.Add(functionSymbol);

        var body = (ProcessFunctionBlock(statement.Body, statement.Parameters) as BoundBlockStatement)!;

        functionSymbol.Parameters = body.Scope.GetLocalParameters();

        return new BoundFunctionDeclarationStatement(statement, body, functionSymbol);
    }




    public BoundStatement ProcessReturn(ReturnStatement statement)
        => new BoundReturnStatement(statement, statement.Expression?.Process(this));




    public BoundStatement ProcessBlock(BlockStatement statement)
        => ProcessLexicalBlock(statement);


    private BoundStatement ProcessLexicalBlock(BlockStatement statement)
        => Scope.ProcessInnerScope(ref _scope, () => ProcessBlockToBound(statement));


    private BoundStatement ProcessFunctionBlock(BlockStatement statement, IEnumerable<FunctionParameterDeclaration> parameters)
        => Scope.ProcessInnerScope(ref _scope, () =>
        {
            DeclareFunctionParameters(parameters);
            return ProcessBlockToBound(statement);
        });


    private void DeclareFunctionParameters(IEnumerable<FunctionParameterDeclaration> parameters)
    {
        foreach (var parameter in parameters)
            Scope.Symbols.Add(new VariableSymbol(parameter.Name, Scope) { IsParameter = true });
    }


    private BoundBlockStatement ProcessBlockToBound(BlockStatement statement)
    {
        var boundStatements = ProcessAll(statement.Statements.ToArray());
        return new BoundBlockStatement(statement, boundStatements, Scope);
    }

    #endregion








    #region Expressions

    public BoundExpression Process(Expression expression)
        => expression.Process(this);


    public IEnumerable<BoundExpression> ProcessAll(IEnumerable<Expression> expressions)
        => expressions.Select(Process).ToArray();




    public BoundExpression ProcessLiteral(LiteralExpression expression)
        => new BoundLiteralExpression(expression);




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
        var variableSymbol = (symbol as VariableSymbol)!;

        if (symbol is null)
            ReportToken(Diagnostic.BinderCatalog.UndeclaredSymbol, expression.Source());

        else if (symbol is not VariableSymbol)
            ReportToken(Diagnostic.BinderCatalog.SymbolIsNotAValue, expression.Source());

        return new BoundSymbolExpression(expression, variableSymbol);
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
    {
        var pointer = Process(expression.Pointer);
        return new BoundPointerAccessExpression(expression, pointer);
    }




    public BoundExpression ProcessCall(CallExpression expression)
    {
        // TODO: check function arity (after add delegates)

        var callee = Process(expression.Callee);
        var arguments = ProcessAll(expression.Arguments.ToArray());

        return new BoundCallExpression(expression, callee, arguments);
    }




    public BoundExpression ProcessCast(CastExpression expression)
    {
        var value = Process(expression.Expression);
        return new BoundCastExpression(expression, value);
    }

    #endregion








    private void ReportIfMultipleDeclaration(Token symbol)
    {
        if (Scope.SymbolExists(symbol.Lexeme))
            ReportToken(Diagnostic.BinderCatalog.MultipleSymbolDeclaration, symbol);
    }
}
