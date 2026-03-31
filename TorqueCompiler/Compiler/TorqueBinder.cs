using System.Collections.Generic;
using System.Linq;
using System.IO;

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
    private FunctionDeclarationStatement? _currentFunction;


    public bool IsInsideALoop => _currentLoopDepth > 0;
    public bool IsInsideAFunction => _currentFunction is not null;
    public bool IsInFileScope => !IsInsideAFunction;
    public bool IsInFunctionScope => IsInsideAFunction;


    private Scope _scope = new Scope();
    public Scope Scope
    {
        get => _scope;
        private set => _scope = value;
    }

    public NamedTypeSyntaxBinder NamedTypeSyntaxBinder { get; }
    public DeclaredTypeManager DeclaredTypes { get; }

    public List<Statement> Statements { get; }

    public TorqueBinderReporter Reporter { get; }

    public string ModulePath { get; }
    public List<Module> ImportedModules { get; }




    public TorqueBinder(IReadOnlyList<Statement> statements, string modulePath)
    {
        Statements = statements.ToList();

        DeclaredTypes = new DeclaredTypeManager();
        NamedTypeSyntaxBinder = new NamedTypeSyntaxBinder(DeclaredTypes);
        Scope = new Scope();
        Reporter = new TorqueBinderReporter(this);

        ModulePath = modulePath;
        ImportedModules = [];
    }




    public Module Bind()
    {
        ImportModules();
        DeclareAllDeclarations();

        var boundStatements = new List<BoundStatement>();

        foreach (var statement in Statements)
            if (Process(statement) is {} boundStatement)
                boundStatements.Add(boundStatement);

        return new Module(ModulePath, boundStatements, Statements, Scope, DeclaredTypes, ImportedModules);
    }


    private void ImportModules()
    {
        foreach (var statement in Statements.ToArray())
        {
            if (statement is not ImportStatement import)
                continue;

            Process(import);
            Statements.Remove(statement);
        }
    }


    private void DeclareAllDeclarations()
    {
        foreach (var statement in Statements.ToArray())
        {
            if (statement is not IDeclaration declaration)
                continue;

            DeclareDeclaration(declaration, statement);
        }
    }


    private void DeclareDeclaration(IDeclaration declaration, Statement statement)
    {
        Process(declaration);

        if (statement is GlobalTypeDeclarationStatement)
            Statements.Remove(statement);
    }




    #region Declarations

    public void Process(IDeclaration declaration)
    {
        declaration.ProcessDeclaration(this);
        Reporter.Process(declaration);
    }




    public void ProcessFunctionDeclaration(FunctionDeclarationStatement declaration)
    {
        var functionSymbol = new FunctionSymbol(declaration.Name, Scope) { IsExternal = declaration.IsExternal };
        Scope.Symbols.Add(functionSymbol);
    }




    public void ProcessAliasDeclaration(AliasDeclarationStatement declaration)
    {
        var aliasDeclaration = BindNamedTypeSyntaxOfAlias(declaration);

        DeclaredTypes.Types.Add(aliasDeclaration);
    }


    private AliasTypeDeclaration BindNamedTypeSyntaxOfAlias(AliasDeclarationStatement declaration)
    {
        var aliasDeclaration = declaration.GetTypeDeclaration();
        aliasDeclaration.TypeSyntax = NamedTypeSyntaxBinder.Process(aliasDeclaration.TypeSyntax);

        return aliasDeclaration;
    }




    public void ProcessStructDeclaration(StructDeclarationStatement declaration)
    {
        DeclaredTypes.Types.Add(declaration.GetTypeDeclaration());
    }

    #endregion




    #region Statements

    public IReadOnlyList<BoundStatement> ProcessAll(IReadOnlyList<Statement> statements)
        => statements.Select(Process).ToArray();


    public BoundStatement Process(Statement statement)
    {
        BoundStatement? boundStatement = null;

        if (!IsFileScopeDeclarationAndHasNotBeenDeclared(statement))
            boundStatement = statement.Process(this);

        Reporter.Process(statement);

        return boundStatement!;
    }


    private bool IsFileScopeDeclarationAndHasNotBeenDeclared(Statement statement)
    {
        return statement is IDeclaration declaration && statement is { CanBeInFunctionScope: false, CanBeInFileScope: true }
                                                     && !Scope.SymbolExists(declaration.Symbol.Name);
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

        _currentFunction = statement;

        var functionSymbol = (Scope.GetSymbol(statement.Name.Name) as FunctionSymbol)!;
        var body = ProcessFunctionBlockIfNotExternal(statement, functionSymbol);

        _currentFunction = null;

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
    {
        var value = statement.Expression is not null ? Process(statement.Expression) : null;

        return new BoundReturnStatement(statement, value);
    }


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




    public BoundStatement ProcessImport(ImportStatement statement)
    {
        var modulePath = Path.Combine(CommandLine.Torque.GetCurrentImportReference(), statement.GetModuleRelativePath());
        var (module, state) = CommandLine.Torque.GetModule(modulePath);

        if (module is not null)
            ImportModule(module.Value);

        return null!;
    }

    private void ImportModule(Module module)
    {
        ImportedModules.Add(module);
        Scope.ImportedScopes.Add(module.Scope);
        DeclaredTypes.ImportedTypeManagers.Add(module.DeclaredTypes);
    }

    #endregion








    #region Expressions

    public BoundExpression Process(Expression expression)
    {
        var boundExpression = expression.Process(this);
        Reporter.Process(expression);

        return boundExpression;
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
        var reference = Process(expression.Expression);

        return new BoundAddressExpression(expression, reference);
    }




    public BoundExpression ProcessAssignment(AssignmentExpression expression)
    {
        var target = Process(expression.Reference);
        var value = Process(expression.Value);

        return new BoundAssignmentExpression(expression, target, value);
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




    public BoundExpression ProcessMemberAccess(MemberAccessExpression expression)
        => new BoundMemberAccessExpression(expression, Process(expression.Compound), expression.Member);

    #endregion
}
