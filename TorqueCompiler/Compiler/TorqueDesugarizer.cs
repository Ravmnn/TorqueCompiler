using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;
using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;


namespace Torque.Compiler;




public class TorqueDesugarizer(IReadOnlyList<Statement> statements)
    : IStatementProcessor<Statement>, IExpressionProcessor<Expression>,
        ISugarStatementProcessor, ISugarExpressionProcessor,
        IGlobalTypeDeclarationProcessor<Statement>
{
    public IReadOnlyList<Statement> Statements { get; } = statements;




    public IReadOnlyList<Statement> Desugarize()
        => Statements.Select(SugarProcess).ToArray();




    private Statement SugarProcess(Statement statement)
        => (this as ISugarStatementProcessor).Process(statement);

    private Statement Process(Statement statement)
        => (this as IStatementProcessor<Statement>).Process(statement);




    Statement ISugarStatementProcessor.Process(Statement statement)
    {
        if (statement is SugarStatement sugarStatement)
            return sugarStatement.Process(this);

        if (statement is GlobalTypeDeclaration declaration)
            return declaration.ProcessGlobalTypeDeclaration(this);

        return statement.Process(this);
    }


    Statement IStatementProcessor<Statement>.Process(Statement statement)
        => statement.Process(this);




    public Statement Process(GlobalTypeDeclaration declaration)
        => declaration.Process(this);




    public Statement ProcessAlias(AliasDeclarationStatement declaration)
        => declaration;




    public Statement ProcessStruct(StructDeclarationStatement declaration)
        => declaration;




    public Statement ProcessExpression(ExpressionStatement statement)
    {
        statement.Expression = SugarProcess(statement.Expression);
        return statement;
    }


    public Statement ProcessVariable(VariableDeclarationStatement statement)
    {
        statement.Value = SugarProcess(statement.Value);
        return statement;
    }


    public Statement ProcessFunction(FunctionDeclarationStatement statement)
    {
        var desugarizedBody = statement.Body is not null ? SugarProcess(statement.Body) : null;
        statement.Body = (desugarizedBody as BlockStatement)!;

        return statement;
    }


    public Statement ProcessReturn(ReturnStatement statement)
    {
        statement.Expression = statement.Expression is not null ? SugarProcess(statement.Expression) : null;
        return statement;
    }


    public Statement ProcessBlock(BlockStatement statement)
    {
        var desugarizedStatements = statement.Statements.Select(SugarProcess).ToArray();
        statement.Statements = desugarizedStatements;

        return statement;
    }


    public Statement ProcessIf(IfStatement statement)
    {
        statement.Condition = SugarProcess(statement.Condition);
        statement.ThenStatement = SugarProcess(statement.ThenStatement);
        statement.ElseStatement = statement.ElseStatement is not null ? SugarProcess(statement.ElseStatement) : null;

        return statement;
    }


    public Statement ProcessWhile(WhileStatement statement)
    {
        statement.Condition = SugarProcess(statement.Condition);
        statement.Loop = SugarProcess(statement.Loop);

        return statement;
    }


    public Statement ProcessBreak(BreakStatement statement) => statement;
    public Statement ProcessContinue(ContinueStatement statement) => statement;








    public Statement ProcessDefaultDeclaration(SugarDefaultDeclarationStatement statement)
    {
        var defaultValue = new DefaultExpression(statement.Type, statement.Location);
        return new VariableDeclarationStatement(statement.Type, statement.Name, defaultValue) { Modifiers = statement.Modifiers };
    }




    public Statement ProcessLoop(SugarLoopStatement statement)
    {
        statement.Body = SugarProcess(statement.Body);

        var literalTrue = new LiteralExpression(true, statement.Location);
        return new WhileStatement(literalTrue, statement.Body, null, statement.Location);
    }




    public Statement ProcessFor(SugarForStatement statement)
    {
        statement.Initialization = SugarProcess(statement.Initialization);
        statement.Condition = SugarProcess(statement.Condition);
        statement.Step = SugarProcess(statement.Step);
        statement.Loop = SugarProcess(statement.Loop);

        return new BlockStatement([
            statement.Initialization,
            new WhileStatement(statement.Condition, statement.Loop,
                new ExpressionStatement(statement.Step), statement.Location)
        ], statement.Location);
    }








    private Expression SugarProcess(Expression expression)
        => (this as ISugarExpressionProcessor).Process(expression);

    private Expression Process(Expression expression)
        => (this as IExpressionProcessor<Expression>).Process(expression);




    Expression ISugarExpressionProcessor.Process(Expression expression)
    {
        if (expression is SugarExpression sugarExpression)
            return sugarExpression.Process(this);

        return expression.Process(this);
    }


    Expression IExpressionProcessor<Expression>.Process(Expression expression)
        => expression.Process(this);




    public Expression ProcessLiteral(LiteralExpression expression)
        => expression;


    public Expression ProcessBinary(BinaryExpression expression)
        => ProcessBinaryLayout(expression);


    public Expression ProcessUnary(UnaryExpression expression)
        => ProcessUnaryLayout(expression);


    public Expression ProcessGrouping(GroupingExpression expression)
    {
        expression.Expression = SugarProcess(expression.Expression);
        return expression;
    }


    public Expression ProcessComparison(ComparisonExpression expression)
        => ProcessBinaryLayout(expression);


    public Expression ProcessEquality(EqualityExpression expression)
        => ProcessBinaryLayout(expression);


    public Expression ProcessLogic(LogicExpression expression)
        => ProcessBinaryLayout(expression);


    public Expression ProcessSymbol(SymbolExpression expression)
        => expression;


    public Expression ProcessAddress(AddressExpression expression)
        => ProcessUnaryLayout(expression);


    public Expression ProcessAssignment(AssignmentExpression expression)
        => ProcessBinaryLayout(expression);


    public Expression ProcessPointerAccess(PointerAccessExpression expression)
        => ProcessUnaryLayout(expression);


    public Expression ProcessCall(CallExpression expression)
    {
        expression.Callee = SugarProcess(expression.Callee);
        expression.Arguments = expression.Arguments.Select(SugarProcess).ToArray();
        return expression;
    }


    public Expression ProcessCast(CastExpression expression)
    {
        expression.Expression = SugarProcess(expression.Expression);
        return expression;
    }


    public Expression ProcessArray(ArrayExpression expression)
    {
        expression.Elements = expression.Elements?.Select(SugarProcess).ToArray();
        return expression;
    }


    public Expression ProcessIndexing(IndexingExpression expression)
    {
        expression.Pointer = SugarProcess(expression.Pointer);
        expression.Index = SugarProcess(expression.Index);
        return expression;
    }


    public Expression ProcessDefault(DefaultExpression expression)
        => expression;


    public Expression ProcessStruct(StructExpression expression)
    {
        for (var index = 0; index < expression.InitializationList.Count; index++)
        {
            var memberInitialization = expression.InitializationList[index];
            expression.InitializationList[index] = memberInitialization with { Value = Process(memberInitialization.Value) };
        }

        return expression;
    }




    private BinaryLayoutExpression ProcessBinaryLayout(BinaryLayoutExpression expression)
    {
        expression.Left = SugarProcess(expression.Left);
        expression.Right = SugarProcess(expression.Right);
        return expression;
    }


    private UnaryLayoutExpression ProcessUnaryLayout(UnaryLayoutExpression expression)
    {
        expression.Right = SugarProcess(expression.Right);
        return expression;
    }








    public Expression ProcessNullptr(SugarNullptrExpression expression)
        => new DefaultExpression(CreateGenericPointerTypeSyntax(expression.Location), expression.Location);


    private static PointerTypeSyntax CreateGenericPointerTypeSyntax(Span location)
    {
        var byteLexeme = Keywords.PrimitiveTypes.First(pair => pair.Value == PrimitiveType.UInt8).Key;
        return new PointerTypeSyntax(new BaseTypeSyntax(new SymbolSyntax(byteLexeme, location)));
    }
}
