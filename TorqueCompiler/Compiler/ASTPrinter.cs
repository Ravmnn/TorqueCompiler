using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

using Torque.Compiler.Tokens;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;


namespace Torque.Compiler;




public class ASTPrinter
    : IExpressionProcessor<string>, IStatementProcessor<string>, IGlobalTypeDeclarationProcessor<string>
{
    private int _indentDegree;




    public string Print(IReadOnlyList<Statement> statements)
        => statements.ItemsToStringThenJoin("", Process);


    public string Print(Statement statement)
        => Print([statement]);


    public string Print(Expression expression)
        => Process(expression);




    private string Stringify(string name, params IReadOnlyList<Expression> expressions)
    {
        if (expressions.Count == 1)
            return UnaryStringify(name, expressions[0]);

        if (expressions.Count == 2)
            return BinaryStringify(name, expressions[0], expressions[1]);

        return MultiOperandStringify(name, expressions);
    }


    private string UnaryStringify(string name, Expression operand)
        => $"({name} {Process(operand)})";


    private string BinaryStringify(string name, Expression left, Expression right)
        => $"({Process(left)} {name} {Process(right)})";


    private string MultiOperandStringify(string name, IReadOnlyList<Expression> expressions)
    {
        var builder = new StringBuilder();
        var processedExpressionsString = expressions.ItemsToStringThenJoin(" ", Process);

        builder.Append($"({name}");
        builder.Append($"{processedExpressionsString})");

        return builder.ToString();
    }


    private void IncreaseIndent()
    {
        _indentDegree++;
    }

    private void DecreaseIndent()
    {
        _indentDegree--;
    }


    private string Indent()
    {
        const int IndentationSize = 4;

        return new string(' ', IndentationSize * _indentDegree);
    }


    private string NewlineChar() => "\n";


    private string BeginStatement()
        => Indent();

    private string EndStatement()
        => NewlineChar();





    public string Process(Statement statement)
    {
        if (statement is GlobalTypeDeclarationStatement declaration)
            return Process(declaration);

        return statement.Process(this);
    }


    public string Process(GlobalTypeDeclarationStatement declaration)
        => declaration.ProcessGlobalTypeDeclaration(this);




    public string ProcessAlias(AliasDeclarationStatement declaration)
        => $"{BeginStatement()}alias {declaration.Symbol.Name} = {declaration.TypeSyntax}{EndStatement()}";




    public string ProcessStruct(StructDeclarationStatement declaration)
    {
        var builder = new StringBuilder();

        builder.Append(BeginStatement());
        builder.AppendLine($"struct {declaration.Symbol}");
        builder.AppendLine("{");

        IncreaseIndent();

        foreach (var member in declaration.Members)
            builder.Append($"{BeginStatement()}{member}{EndStatement()}");

        DecreaseIndent();

        builder.AppendLine("}");
        builder.Append(EndStatement());

        return builder.ToString();
    }




    public string ProcessExpression(ExpressionStatement statement)
        => $"{BeginStatement()}{Process(statement.Expression)}{EndStatement()}";




    public string ProcessVariableDefinition(VariableDeclarationStatement statement)
        => $"{BeginStatement()}{statement.Type} {statement.Name.Name} = {Process(statement.Value)} {EndStatement()}";




    public string ProcessFunctionDefinition(FunctionDeclarationStatement statement)
    {
        var builder = new StringBuilder();
        var parametersString = statement.Parameters.ItemsToStringThenJoin(", ", param => $"{param.Type} {param.Name}");

        var externalString = statement.IsExternal ? "external " : "";
        var blockString = statement.IsExternal ? "" : Process(statement.Body!);

        builder.Append($"{BeginStatement()}{externalString}{statement.ReturnType} {statement.Name}({parametersString}){NewlineChar()}");
        builder.Append($"{blockString}{EndStatement()}");

        return builder.ToString();
    }




    public string ProcessReturn(ReturnStatement statement)
    {
        var expressionString = statement.Expression is null ? "" : $" {Process(statement.Expression)}";
        return $"{BeginStatement()}return{expressionString}{EndStatement()}";
    }


    public string ProcessBlock(BlockStatement blockStatement)
    {
        var builder = new StringBuilder();

        builder.Append($"{BeginStatement()}{{{NewlineChar()}");
        IncreaseIndent();

        var statementsString = blockStatement.Statements.ItemsToStringThenJoin("", Process);
        builder.Append(statementsString);

        DecreaseIndent();
        builder.Append($"{Indent()}}}{EndStatement()}");

        return builder.ToString();
    }




    public string ProcessIf(IfStatement statement)
    {
        var builder = new StringBuilder();

        builder.Append($"{BeginStatement()}if {Process(statement.Condition)}{NewlineChar()}");
        builder.Append(ForIndentDo(statement.ThenStatement));

        if (statement.ElseStatement is not null)
        {
            builder.Append($"{BeginStatement()}else{NewlineChar()}");
            builder.Append(ForIndentDo(statement.ElseStatement));
        }

        return builder.ToString();
    }




    public string ProcessWhile(WhileStatement statement)
    {
        var builder = new StringBuilder();

        builder.Append($"{BeginStatement()}while {Process(statement.Condition)}{NewlineChar()}");
        builder.Append(ForIndentDo(statement.Loop));

        return builder.ToString();
    }


    public string ProcessBreak(BreakStatement statement)
        => "break";


    public string ProcessContinue(ContinueStatement statement)
        => "continue";








    public string Process(Expression expression)
        => expression.Process(this);




    public string ProcessLiteral(LiteralExpression expression) => expression.Value switch
    {
        IReadOnlyList<byte> @string => $"\"{ByteListToString(@string)}\"",
        byte @byte => $"'{ByteListToString([@byte])}'",

        ulong or double or bool => expression.Value.ToString()!,

        _ => throw new UnreachableException()
    };


    private static string ByteListToString(IReadOnlyList<byte> @string)
        => Encoding.ASCII.GetString(@string.ToArray());




    public string ProcessBinary(BinaryExpression expression)
        => Stringify(OperatorFromTokenType(expression.Operator), expression.Left, expression.Right);




    public string ProcessGrouping(GroupingExpression expression)
        => $"({Process(expression.Expression)})";




    public string ProcessComparison(ComparisonExpression expression)
        => Stringify(OperatorFromTokenType(expression.Operator), expression.Left, expression.Right);




    public string ProcessEquality(EqualityExpression expression)
        => Stringify(OperatorFromTokenType(expression.Operator), expression.Left, expression.Right);




    public string ProcessLogic(LogicExpression expression)
        => Stringify(OperatorFromTokenType(expression.Operator), expression.Left, expression.Right);




    public string ProcessSymbol(SymbolExpression expression)
        => $"({expression.Symbol.Name})";




    public string ProcessAddress(AddressExpression expression)
        => Stringify(OperatorFromTokenType(expression.Operator), expression.Expression);




    public string ProcessUnary(UnaryExpression expression)
        => Stringify(OperatorFromTokenType(expression.Operator), expression.Right);




    public string ProcessAssignment(AssignmentExpression expression)
        => Stringify("=", expression.Target, expression.Value);




    public string ProcessPointerAccess(PointerAccessExpression expression)
        => Stringify("*", expression.Pointer);




    public string ProcessCall(CallExpression expression)
    {
        var builder = new StringBuilder();
        var argumentsString = expression.Arguments.ItemsToStringThenJoin(", ", Process);

        builder.Append($"{Process(expression.Callee)}({argumentsString})");

        return builder.ToString();
    }




    public string ProcessCast(CastExpression expression)
        => $"({Process(expression.Expression)} as {expression.Type})";




    public string ProcessArray(ArrayExpression expression)
    {
        var expressionsString = expression.Elements?.Select(Process);
        var elementsString = expressionsString?.ItemsToStringThenJoin(", ", item => item);
        var initializationListString = expressionsString is not null ? $" {{ {elementsString} }}" : "";

        return $"({expression.ElementType} array[{expression.Length}]{initializationListString})";
    }




    public string ProcessIndexing(IndexingExpression expression)
        => $"({Process(expression.Pointer)}[{Process(expression.Index)}])";




    public string ProcessDefault(DefaultExpression expression)
        => $"(defaultFor {expression.TypeSyntax})";




    public string ProcessStruct(StructExpression expression)
    {
        var initializationListAsString = expression.InitializationList.ItemsToStringThenJoin(",",
            member => $"{member.Member}: {Process(member.Value)}");

        return $"new {expression.Symbol} {{ {initializationListAsString} }}";
    }




    public string ProcessMemberAccess(MemberAccessExpression expression)
        => $"({Process(expression.Compound)}.{expression.Member.Name})";




    private string ForIndentDo(Statement statement)
    {
        var isBlock = statement is BlockStatement;

        if (!isBlock)
            IncreaseIndent();

        var result= Process(statement);

        if (!isBlock)
            DecreaseIndent();

        return result;
    }








    private static string OperatorFromTokenType(TokenType type) => type switch
    {
        TokenType.Plus => "+",
        TokenType.Minus => "-",
        TokenType.Star => "*",
        TokenType.Slash => "/",
        TokenType.Ampersand => "&",

        TokenType.GreaterThan => ">",
        TokenType.GreaterThanOrEqual => ">=",
        TokenType.LessThan => "<",
        TokenType.LessThanOrEqual => "<=",

        TokenType.Equality => "==",
        TokenType.Inequality => "!=",

        TokenType.LogicAnd => "&&",
        TokenType.LogicOr => "||",

        _ => throw new UnreachableException()
    };
}
