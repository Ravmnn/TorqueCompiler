using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;


namespace Torque.Compiler;




public class ASTPrinter : IExpressionProcessor<string>, IStatementProcessor<string>
{
    private uint _indentDegree;




    public string Print(IReadOnlyList<Statement> statements)
    {
        var builder = new StringBuilder();

        foreach (var statement in statements)
            builder.Append(Process(statement));

        return builder.ToString();
    }


    public string Print(Statement statement)
        => Print([statement]);



    public string Print(Expression expression)
        => Process(expression);




    private string Stringify(string name, IReadOnlyList<Expression> expressions)
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

        builder.Append($"({name}");

        foreach (var expression in expressions)
        {
            builder.Append(' ');
            Process(expression);
        }

        builder.Append(')');

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
        var indent = "";

        for (var i = 0; i < _indentDegree; i++)
            indent += "    ";

        return indent;
    }


    private string NewlineChar() => "\n";


    private string BeginStatement()
        => Indent();

    private string EndStatement()
        => NewlineChar();





    public string Process(Statement statement)
        => statement.Process(this);




    public string ProcessExpression(ExpressionStatement statement)
        => $"{BeginStatement()}{Process(statement.Expression)}{EndStatement()}";




    public string ProcessDeclaration(DeclarationStatement statement)
        => $"{BeginStatement()}{statement.Type} {statement.Name.Name} = {Process(statement.Value)} {EndStatement()}";




    public string ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        var builder = new StringBuilder();

        builder.Append($"{BeginStatement()}{statement.ReturnType} {statement.Name}(");
        builder.Append(JoinWithComma(statement.Parameters, param => $"{param.Type} {param.Name}"));
        builder.Append($") {NewlineChar()}");
        builder.Append($"{Process(statement.Body)}{EndStatement()}");

        return builder.ToString();
    }




    public string ProcessReturn(ReturnStatement statement)
        => $"{BeginStatement()}return{(statement.Expression is null ? "" : $" {Process(statement.Expression)}")}{EndStatement()}";




    public string ProcessBlock(BlockStatement blockStatement)
    {
        var builder = new StringBuilder();

        builder.Append($"{BeginStatement()}{{{NewlineChar()}");
        IncreaseIndent();

        foreach (var statement in blockStatement.Statements)
            builder.Append(Process(statement));

        DecreaseIndent();
        builder.Append($"{Indent()}}}{EndStatement()}");

        return builder.ToString();
    }




    public string ProcessIf(IfStatement statement)
    {
        var builder = new StringBuilder();

        builder.Append($"{BeginStatement()}if {Process(statement.Condition)}{NewlineChar()}");

        IncreaseIndent();
        builder.Append(Process(statement.ThenStatement));
        DecreaseIndent();

        if (statement.ElseStatement is not null)
        {
            builder.Append($"{BeginStatement()}else{NewlineChar()}");

            IncreaseIndent();
            builder.Append(Process(statement.ElseStatement));
            DecreaseIndent();
        }

        return builder.ToString();
    }




    public string Process(Expression expression)
        => expression.Process(this);




    public string ProcessLiteral(LiteralExpression expression)
        => expression.Value.ToString() ?? "invalid";




    public string ProcessBinary(BinaryExpression expression)
        => Stringify(OperatorFromTokenType(expression.Operator), [expression.Left, expression.Right]);




    public string ProcessGrouping(GroupingExpression expression)
        => $"({Process(expression.Expression)})";




    public string ProcessComparison(ComparisonExpression expression)
        => BinaryStringify(OperatorFromTokenType(expression.Operator), expression.Left, expression.Right);




    public string ProcessEquality(EqualityExpression expression)
        => BinaryStringify(OperatorFromTokenType(expression.Operator), expression.Left, expression.Right);




    public string ProcessLogic(LogicExpression expression)
        => BinaryStringify(OperatorFromTokenType(expression.Operator), expression.Left, expression.Right);




    public string ProcessSymbol(SymbolExpression expression)
        => $"({expression.Symbol.Name})";




    public string ProcessAddress(AddressExpression expression)
        => $"(&{Process(expression.Expression)})";




    public string ProcessUnary(UnaryExpression expression)
        => UnaryStringify(OperatorFromTokenType(expression.Operator), expression.Right);




    public string ProcessAssignment(AssignmentExpression expression)
        => BinaryStringify("=", expression.Target, expression.Value);




    public string ProcessPointerAccess(PointerAccessExpression expression)
        => UnaryStringify("*", expression.Pointer);




    public string ProcessCall(CallExpression expression)
    {
        var builder = new StringBuilder();

        builder.Append($"{Process(expression.Callee)}(");
        builder.Append(JoinWithComma(expression.Arguments, Process));
        builder.Append(')');

        return builder.ToString();
    }




    public string ProcessCast(CastExpression expression)
        => $"({Process(expression.Expression)} as {expression.Type})";




    public string ProcessArray(ArrayExpression expression)
    {
        var expressionsString = expression.Elements?.Select(Process);
        var elementsString = expressionsString is not null ? $" {{ {JoinWithComma(expressionsString.ToList(), str => str)} }}" : "";

        return $"({expression.ElementType} array[{expression.Size}]{elementsString})";
    }




    public string ProcessIndexing(IndexingExpression expression)
        => $"({Process(expression.Pointer)}[{Process(expression.Index)}])";




    public string ProcessDefault(DefaultExpression expression)
        => $"(defaultFor {expression.TypeName})";








    private static string JoinWithComma<T>(IReadOnlyList<T> elements, Func<T, string> processor)
    {
        var builder = new StringBuilder();

        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];
            var atEnd = i + 1 >= elements.Count;

            builder.Append($"{processor(element)}{(!atEnd ? ", " : "")}");
        }

        return builder.ToString();
    }








    private static string OperatorFromTokenType(TokenType type) => type switch
    {
        TokenType.Plus => "+",
        TokenType.Minus => "-",
        TokenType.Star => "*",
        TokenType.Slash => "/",

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
