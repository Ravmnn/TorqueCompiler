using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Symbols;
using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler;




public class TorqueBinder :
    IExpressionProcessor<BoundExpression>, IStatementProcessor<BoundStatement>,
    IDeclarationProcessor
{
    private int _currentLoopDepth;


    public bool IsInsideALoop => _currentLoopDepth > 0;

    private Scope _scope = new Scope();
    public Scope Scope
    {
        get => _scope;
        private set => _scope = value;
    }

    public DeclaredTypeManager DeclaredTypes { get; private set; } = new DeclaredTypeManager();
    public List<Statement> Statements { get; }

    public TorqueBinderReporter Reporter { get; private set; }




    public TorqueBinder(IReadOnlyList<Statement> statements)
    {
        Statements = statements.ToList();

        Reporter = new TorqueBinderReporter(this);
    }




    public IReadOnlyList<BoundStatement> Bind()
    {
        Reset();

        DeclareAllDeclarations();
        return Statements.Select(Process).ToArray();
    }


    private void Reset()
    {
        _currentLoopDepth = 0;
        DeclaredTypes = new DeclaredTypeManager();

        Scope = new Scope();
        Reporter = new TorqueBinderReporter(this);

        // TODO: remove these Reset methods from all the tools, state recovery should be achieved by recreating the class
    }


    private void DeclareAllDeclarations()
    {
        foreach (var statement in Statements.ToArray())
        {
            if (statement is not IDeclaration declaration)
                continue;

            Process(declaration);

            if (statement is GlobalTypeDeclaration)
                Statements.Remove(statement);
        }
    }




    #region Declarations

    public void Process(IDeclaration declaration)
    {
        Reporter.Process(declaration);
        declaration.ProcessDeclaration(this);
    }




    public void ProcessFunctionDeclaration(FunctionDeclarationStatement declaration)
    {
        var functionSymbol = new FunctionSymbol(declaration.Name, Scope) { IsExternal = declaration.IsExternal };
        Scope.Symbols.Add(functionSymbol);
    }




    public void ProcessAliasDeclaration(AliasDeclarationStatement declaration)
    {
        // TODO: create "GlobalTypeDeclaration.GetTypeDeclaration()"... use this also in the binder reporter, struct declaration processing
        DeclaredTypes.Types.Add(new AliasTypeDeclaration(declaration.Symbol, declaration.TypeSyntax));
    }




    public void ProcessStructDeclaration(StructDeclarationStatement declaration)
    {
        var structType = new StructTypeDeclaration(declaration.Symbol, declaration.Members);
        DeclaredTypes.Types.Add(structType);
    }

    #endregion




    #region Statements

    public IReadOnlyList<BoundStatement> ProcessAll(IReadOnlyList<Statement> statements)
        => statements.Select(Process).ToArray();


    public BoundStatement Process(Statement statement)
    {
        Reporter.Process(statement);
        return statement.Process(this);
    }




    public BoundStatement ProcessExpression(ExpressionStatement statement)
        => new BoundExpressionStatement(statement, Process(statement.Expression));




    public BoundStatement ProcessVariableDefinition(VariableDeclarationStatement statement)
    {
        var identifier = new VariableSymbol(statement.Name, Scope);
        var value = Process(statement.Value);

        Scope.Symbols.Add(identifier);

        return new BoundVariableDeclarationStatement(statement, identifier, value);
    }




    public BoundStatement ProcessFunctionDefinition(FunctionDeclarationStatement statement)
    {
        // BUG: multiple declarations for parameters and struct fields are allowed

        var functionSymbol = (Scope.GetSymbol(statement.Name.Name) as FunctionSymbol)!;
        var body = ProcessFunctionBlockIfNotExternal(statement, functionSymbol);

        return new BoundFunctionDeclarationStatement(statement, body, functionSymbol);
    }


    private BoundBlockStatement? ProcessFunctionBlockIfNotExternal(FunctionDeclarationStatement statement, FunctionSymbol functionSymbol)
    {
        // when a function is marked as external, its parameter symbols are not going to be used,
        // so declaring then is also unnecessary

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


    private BoundStatement ProcessFunctionBlockAndDeclareParameters(BlockStatement statement, IReadOnlyList<GenericDeclaration> parameters)
        => Scope.ForInnerScopeDo(ref _scope, () =>
        {
            DeclareFunctionParameters(parameters);
            return ProcessBlockToBound(statement);
        });


    private void DeclareFunctionParameters(IReadOnlyList<GenericDeclaration> parameters)
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
        return new BoundBreakStatement(statement);
    }


    public BoundStatement ProcessContinue(ContinueStatement statement)
    {
        return new BoundContinueStatement(statement);
    }

    #endregion








    #region Expressions

    public BoundExpression Process(Expression expression)
    {
        Reporter.Process(expression);
        return expression.Process(this);
    }


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

        return new BoundSymbolExpression(expression, variableSymbol);
    }




    public BoundExpression ProcessAddress(AddressExpression expression)
    {
        var target = Process(expression.Expression);
        var addressable = ToAddressable(target);

        return new BoundAddressExpression(expression, addressable);
    }


    private static BoundAddressableExpression ToAddressable(BoundExpression expression)
        => new BoundAddressableExpression(expression.Syntax, expression);




    public BoundExpression ProcessAssignment(AssignmentExpression expression)
    {
        var target = Process(expression.Target);
        var reference = ToAssignmentReference(target);

        var value = Process(expression.Value);

        return new BoundAssignmentExpression(expression, reference, value);
    }


    private static BoundAssignmentReferenceExpression ToAssignmentReference(BoundExpression expression)
        => new BoundAssignmentReferenceExpression(expression.Syntax, expression);




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
    {
        return new BoundDefaultExpression(expression);
    }




    public BoundExpression ProcessStruct(StructExpression expression)
    {
        var boundMembersInitialization = new List<BoundStructMemberInitialization>();

        foreach (var member in expression.InitializationList)
            boundMembersInitialization.Add(new BoundStructMemberInitialization(member.Member, Process(member.Value)));

        return new BoundStructExpression(boundMembersInitialization, expression);
    }

    #endregion
}
