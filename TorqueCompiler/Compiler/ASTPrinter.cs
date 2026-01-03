using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Torque.Compiler;




public class ASTPrinter : IExpressionProcessor<string>, IStatementProcessor<string>
{
    private uint _indentDegree;


    public bool FoldBlocks { get; set; }
    public bool NoNewlines { get; set; }




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


    private string NewlineChar()
    {
        return NoNewlines ? "" : "\n";
    }



    private string BeginStatement()
        => Indent();

    private string EndStatement()
        => NewlineChar();





    public string Process(Statement statement)
        => statement.Process(this);




    public string ProcessExpression(ExpressionStatement statement)
        => $"{BeginStatement()}{Process(statement.Expression)}{EndStatement()}";




    public string ProcessDeclaration(DeclarationStatement statement)
        => $"{BeginStatement()}{statement.Type} {statement.Name.Lexeme} = {Process(statement.Value)} {EndStatement()}";




    public string ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        var builder = new StringBuilder();

        builder.Append(BeginStatement());
        builder.Append($"{statement.ReturnType} {statement.Name.Lexeme}(");

        var parameters = statement.Parameters;

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var atEnd = i + 1 >= parameters.Count;

            builder.Append($"{parameter.Type} {parameter.Name.Lexeme}{(!atEnd ? ", " : "")}");
        }

        builder.Append($") {NewlineChar()}");
        builder.Append(Process(statement.Body));
        builder.Append(EndStatement());

        return builder.ToString();
    }




    public string ProcessReturn(ReturnStatement statement)
        => $"{BeginStatement()}return{(statement.Expression is null ? "" : $" {Process(statement.Expression)}")}{EndStatement()}";




    public string ProcessBlock(BlockStatement blockStatement)
    {
        var builder = new StringBuilder();

        if (FoldBlocks)
        {
            builder.Append("block...");
            return builder.ToString();
        }

        builder.Append(BeginStatement());
        builder.Append($"{{{NewlineChar()}");

        IncreaseIndent();

        foreach (var statement in blockStatement.Statements)
            builder.Append(Process(statement));

        DecreaseIndent();

        builder.Append($"{Indent()}}}");
        builder.Append(EndStatement());

        return builder.ToString();
    }







    public string Process(Expression expression)
        => expression.Process(this);


    public string ProcessLiteral(LiteralExpression expression)
        => expression.Value.Lexeme;


    public string ProcessBinary(BinaryExpression expression)
        => Stringify(expression.Operator.Lexeme, [expression.Left, expression.Right]);


    public string ProcessGrouping(GroupingExpression expression)
        => $"({Process(expression.Expression)})";


    public string ProcessComparison(ComparisonExpression expression)
        => BinaryStringify(expression.Operator.Lexeme, expression.Left, expression.Right);


    public string ProcessEquality(EqualityExpression expression)
        => BinaryStringify(expression.Operator.Lexeme, expression.Left, expression.Right);


    public string ProcessLogic(LogicExpression expression)
        => BinaryStringify(expression.Operator.Lexeme, expression.Left, expression.Right);


    public string ProcessSymbol(SymbolExpression expression)
        => $"({(expression.GetAddress ? "&" : "$")}{expression.Identifier.Lexeme})";


    public string ProcessUnary(UnaryExpression expression)
        => UnaryStringify(expression.Operator.Lexeme, expression.Right);


    public string ProcessAssignment(AssignmentExpression expression)
        => BinaryStringify("=", expression.Pointer, expression.Value);


    public string ProcessPointerAccess(PointerAccessExpression expression)
        => UnaryStringify("*", expression.Pointer);


    public string ProcessCall(CallExpression expression)
    {
        var builder = new StringBuilder();

        builder.Append(Process(expression.Callee));
        builder.Append('(');

        var arguments = expression.Arguments;

        for (var i = 0; i < arguments.Count; i++)
        {
            var argument = arguments[i];
            var atEnd = i + 1 >= arguments.Count;

            builder.Append($"{Process(argument)}{(!atEnd ? ", " : "")}");
        }

        builder.Append(')');

        return builder.ToString();
    }


    public string ProcessCast(CastExpression expression)
        => $"({Process(expression.Expression)} as {expression.Type})";


    public string ProcessArray(ArrayExpression expression)
    {
        var expressionsString = expression.Elements.Select(Process);
        return $"({expression.ElementType} array[{expression.Size}] {{ {string.Join(", ", expressionsString)} }})";
    }


    public string ProcessIndexing(IndexingExpression expression)
        => $"({Process(expression.Pointer)}[{Process(expression.Index)}])";


    public string ProcessDefault(DefaultExpression expression)
        => $"(defaultOf {expression.TypeName})";
}
