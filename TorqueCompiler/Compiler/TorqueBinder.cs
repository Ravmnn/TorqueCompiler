using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler;




public class TorqueBinder(IReadOnlyList<Statement> statements) : DiagnosticReporter<BinderCatalog>,
    IExpressionProcessor<BoundExpression>, IStatementProcessor<BoundStatement>,
    IDeclarationProcessor
{
    private int _currentLoopDepth;
    private bool IsInsideALoop => _currentLoopDepth > 0;

    private Scope _scope = new Scope();
    public Scope Scope
    {
        get => _scope;
        private set => _scope = value;
    }


    public IReadOnlyList<Statement> Statements { get; } = statements;




    public IReadOnlyList<BoundStatement> Bind()
    {
        Reset();

        DeclareAllDeclarations();
        return Statements.Select(Process).ToArray();
    }


    private void Reset()
    {
        _currentLoopDepth = 0;

        Scope = new Scope();
        Diagnostics.Clear();
    }


    private void DeclareAllDeclarations()
    {
        foreach (var statement in Statements)
            if (statement is IDeclaration declaration)
                Process(declaration);
    }




    #region Declarations

    public void Process(IDeclaration declaration)
    {
        ReportIfMultipleDeclaration(declaration.Symbol);
        declaration.ProcessDeclaration(this);
    }




    public void ProcessVariableDeclaration(VariableDeclarationStatement declaration)
    {
        // variables cannot be currently globally declared
        throw new UnreachableException();
    }




    public void ProcessFunctionDeclaration(FunctionDeclarationStatement declaration)
    {
        var functionSymbol = new FunctionSymbol(declaration.Name, Scope) { IsExternal = declaration.IsExternal };
        Scope.Symbols.Add(functionSymbol);
    }

    #endregion




    #region Statements

    public BoundStatement Process(Statement statement)
    {
        if (ReportIfNonDeclarationAtFileScope(statement) || ReportIfFunctionDeclarationAtLocalScope(statement))
            return null!;

        ValidateDeclarationModifiers(statement);

        return statement.Process(this);
    }


    public IReadOnlyList<BoundStatement> ProcessAll(IReadOnlyList<Statement> statements)
        => statements.Select(Process).ToArray();




    public BoundStatement ProcessExpression(ExpressionStatement statement)
        => new BoundExpressionStatement(statement, Process(statement.Expression));




    public BoundStatement ProcessVariable(VariableDeclarationStatement statement)
    {
        ReportIfMultipleDeclaration(statement.Name);

        var identifier = new VariableSymbol(statement.Name, Scope);
        var value = Process(statement.Value);

        Scope.Symbols.Add(identifier);

        return new BoundVariableDeclarationStatement(statement, identifier, value);
    }




    public BoundStatement ProcessFunction(FunctionDeclarationStatement statement)
    {
        var functionSymbol = (Scope.GetSymbol(statement.Name.Name) as FunctionSymbol)!;
        var body = ProcessFunctionBlockIfNotExternal(statement, functionSymbol);

        return new BoundFunctionDeclarationStatement(statement, body, functionSymbol);
    }


    private BoundBlockStatement? ProcessFunctionBlockIfNotExternal(FunctionDeclarationStatement statement, FunctionSymbol functionSymbol)
    {
        // when a function is marked as external, its parameter symbols are not going to be used,
        // so declaring then is also unnecessary

        ValidateFunctionBody(statement);

        if (statement.IsExternal || statement.Body is null)
            return null;

        var body = (ProcessFunctionBlockAndDeclareParameters(statement.Body!, statement.Parameters) as BoundBlockStatement)!;
        functionSymbol.Parameters = body.Scope.GetLocalParameters();

        return body;
    }


    public BoundStatement ProcessReturn(ReturnStatement statement)
        => new BoundReturnStatement(statement, statement.Expression?.Process(this));




    public BoundStatement ProcessBlock(BlockStatement statement)
        => ProcessLexicalBlock(statement);


    private BoundStatement ProcessLexicalBlock(BlockStatement statement)
        => Scope.ForInnerScopeDo(ref _scope, () => ProcessBlockToBound(statement));


    private BoundStatement ProcessFunctionBlockAndDeclareParameters(BlockStatement statement, IReadOnlyList<FunctionParameterDeclaration> parameters)
        => Scope.ForInnerScopeDo(ref _scope, () =>
        {
            DeclareFunctionParameters(parameters);
            return ProcessBlockToBound(statement);
        });


    private void DeclareFunctionParameters(IReadOnlyList<FunctionParameterDeclaration> parameters)
    {
        foreach (var parameter in parameters)
            Scope.Symbols.Add(new VariableSymbol(parameter.Name, Scope) { IsParameter = true });
    }


    private BoundBlockStatement ProcessBlockToBound(BlockStatement statement)
    {
        var boundStatements = ProcessAll(statement.Statements.ToArray());
        return new BoundBlockStatement(statement, boundStatements, Scope);
    }




    public BoundStatement ProcessIf(IfStatement statement)
    {
        var condition = Process(statement.Condition);
        var thenStatement = Process(statement.ThenStatement);
        var elseStatement = statement.ElseStatement is not null ? Process(statement.ElseStatement) : null;

        return new BoundIfStatement(statement, condition, thenStatement, elseStatement);
    }




    public BoundStatement ProcessWhile(WhileStatement statement)
    {
        _currentLoopDepth++;

        var condition = Process(statement.Condition);
        var body = Process(statement.Loop);
        var postBody = statement.PostLoop is not null ? Process(statement.PostLoop) : null;

        _currentLoopDepth--;

        return new BoundWhileStatement(statement, condition, body, postBody);
    }


    public BoundStatement ProcessBreak(BreakStatement statement)
    {
        ReportIfNotInsideALoop(statement);
        return new BoundBreakStatement(statement);
    }


    public BoundStatement ProcessContinue(ContinueStatement statement)
    {
        ReportIfNotInsideALoop(statement);
        return new BoundContinueStatement(statement);
    }

    #endregion








    #region Expressions

    public BoundExpression Process(Expression expression)
        => expression.Process(this);


    public IReadOnlyList<BoundExpression> ProcessAll(IReadOnlyList<Expression> expressions)
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
        var boundExpression = Process(expression.Right);
        return new BoundUnaryExpression(expression, boundExpression);
    }




    public BoundExpression ProcessGrouping(GroupingExpression expression)
    {
        var boundExpression = Process(expression.Expression);
        return new BoundGroupingExpression(expression, boundExpression);
    }




    public BoundExpression ProcessComparison(ComparisonExpression expression)
        => new BoundComparisonExpression(expression, Process(expression.Left), Process(expression.Right));


    public BoundExpression ProcessEquality(EqualityExpression expression)
        => new BoundEqualityExpression(expression, Process(expression.Left), Process(expression.Right));


    public BoundExpression ProcessLogic(LogicExpression expression)
        => new BoundLogicExpression(expression, Process(expression.Left), Process(expression.Right));




    public BoundExpression ProcessSymbol(SymbolExpression expression)
    {
        var symbol = Scope.TryGetSymbol(expression.Symbol.Name);
        var variableSymbol = (symbol as VariableSymbol)!;

        if (symbol is null)
            ReportSymbol(BinderCatalog.UndeclaredSymbol, expression.Symbol);

        else if (symbol is not VariableSymbol)
            ReportSymbol(BinderCatalog.SymbolIsNotAValue, expression.Symbol);

        return new BoundSymbolExpression(expression, variableSymbol);
    }




    public BoundExpression ProcessAddress(AddressExpression expression)
    {
        var target = Process(expression.Expression);
        var addressable = ToAddressable(target);

        return new BoundAddressExpression(expression, addressable);
    }


    private BoundAddressableExpression ToAddressable(BoundExpression expression)
    {
        if (expression is not BoundSymbolExpression and not BoundIndexingExpression)
            Report(BinderCatalog.ValueMustBeAddressable, location: expression.Location);

        return new BoundAddressableExpression(expression.Syntax, expression);
    }




    public BoundExpression ProcessAssignment(AssignmentExpression expression)
    {
        var target = Process(expression.Target);
        var reference = ToAssignmentReference(target);

        var value = Process(expression.Value);

        return new BoundAssignmentExpression(expression, reference, value);
    }


    private BoundAssignmentReferenceExpression ToAssignmentReference(BoundExpression expression)
    {
        if (expression is not BoundSymbolExpression and not BoundPointerAccessExpression and not BoundIndexingExpression)
            Report(BinderCatalog.MustBeAssignmentReference, location: expression.Location);

        return new BoundAssignmentReferenceExpression(expression.Syntax, expression);
    }




    public BoundExpression ProcessPointerAccess(PointerAccessExpression expression)
    {
        var pointer = Process(expression.Pointer);
        return new BoundPointerAccessExpression(expression, pointer);
    }




    public BoundExpression ProcessCall(CallExpression expression)
    {
        var callee = Process(expression.Callee);
        var arguments = ProcessAll(expression.Arguments.ToArray());

        return new BoundCallExpression(expression, callee, arguments);
    }




    public BoundExpression ProcessCast(CastExpression expression)
    {
        var value = Process(expression.Expression);
        return new BoundCastExpression(expression, value);
    }




    public BoundExpression ProcessArray(ArrayExpression expression)
    {
        var boundExpressions = expression.Elements?.Select(Process).ToArray();
        return new BoundArrayExpression(expression, boundExpressions);
    }




    public BoundExpression ProcessIndexing(IndexingExpression expression)
        => new BoundIndexingExpression(expression, Process(expression.Pointer), Process(expression.Index));




    public BoundExpression ProcessDefault(DefaultExpression expression)
        => new BoundDefaultExpression(expression);

    #endregion








    #region Diagnostic Reporting

    private bool ReportIfMultipleDeclaration(SymbolSyntax symbol)
    {
        if (!Scope.SymbolExists(symbol.Name))
            return false;

        ReportSymbol(BinderCatalog.MultipleSymbolDeclaration, symbol);
        return true;
    }


    private bool ReportIfNonDeclarationAtFileScope(Statement statement)
    {
        if (!Scope.IsGlobal || statement is FunctionDeclarationStatement)
            return false;

        Report(BinderCatalog.OnlyDeclarationsCanExistInFileScope, location: statement.Location);
        return true;
    }


    private bool ReportIfFunctionDeclarationAtLocalScope(Statement statement)
    {
        if (Scope.IsGlobal || statement is not FunctionDeclarationStatement)
            return false;

        Report(BinderCatalog.FunctionsMustBeAtFileScope, location: statement.Location);
        return true;
    }


    private void ValidateDeclarationModifiers(Statement declaration)
    {
        if (declaration is not IModificable modificable)
            return;

        ReportIfHasDuplicateModifers(declaration, modificable);

        foreach (var modifier in modificable.Modifiers)
            ReportIfInvalidModifierTarget(modificable, modifier);
    }


    private bool ReportIfHasDuplicateModifers(Statement declaration, IModificable modificable)
    {
        if (modificable.Modifiers.DistinctBy(modifier => modifier.Type).Count() == modificable.Modifiers.Count)
            return false;

        Report(BinderCatalog.MultipleSameModifiers, location: declaration.Location);
        return true;
    }


    private bool ReportIfInvalidModifierTarget(IModificable modificable, Modifier modifier)
    {
        var modifierTargets = ModifiersTarget.GetFor(modifier);

        if (modifierTargets.Any(target => target.HasFlag(modificable.ThisTargetIdentity)))
            return false;

        Report(BinderCatalog.InvalidModifierTarget, location: modifier.Location);
        return true;
    }


    private void ValidateFunctionBody(FunctionDeclarationStatement statement)
    {
        var isExternal = statement.IsExternal;
        var hasBody = statement.Body is not null;

        if (isExternal && hasBody)
            Report(BinderCatalog.ExternalFunctionCannotHaveABody, location: statement.Location);

        else if (!isExternal && !hasBody)
            Report(BinderCatalog.FunctionMustHaveABody, location: statement.Location);
    }


    private bool ReportIfNotInsideALoop(Statement statement)
    {
        if (IsInsideALoop)
            return false;

        Report(BinderCatalog.LoopControlInstructionMustBeInLoop, location: statement.Location);
        return true;
    }

    #endregion
}
