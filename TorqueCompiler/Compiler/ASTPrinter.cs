using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Torque.Compiler;




public class ASTPrinter : IExpressionProcessor, IStatementProcessor
{
    private readonly StringBuilder _builder = new StringBuilder();

    private uint _indentDegree;


    public bool IgnoreBlocks { get; set; }
    public bool NoNewlines { get; set; }




    public string Print(IEnumerable<Statement> statements)
    {
        _builder.Clear();

        foreach (var statement in statements)
            Process(statement);

        return _builder.ToString();
    }


    public string Print(Statement statement)
    {
        _builder.Clear();

        Process(statement);

        return _builder.ToString();
    }


    public string Print(Expression expression)
    {
        _builder.Clear();

        Process(expression);

        return _builder.ToString();
    }



    private void Parenthesize(Expression expression)
    {
        _builder.Append('(');

        Process(expression);

        _builder.Append(')');
    }


    private void Stringify(string name, Expression[] expressions)
    {
        if (expressions.Length == 1)
            UnaryStringify(name, expressions[0]);

        else if (expressions.Length == 2)
            BinaryStringify(name, expressions[0], expressions[1]);

        else if (expressions.Length >= 2)
            MultiOperandStringify(name, expressions);
    }


    private void UnaryStringify(string name, Expression operand)
    {
        _builder.Append($"({name} ");

        Process(operand);

        _builder.Append(')');
    }


    private void BinaryStringify(string name, Expression left, Expression right)
    {
        _builder.Append('(');
        Process(left);

        _builder.Append($" {name} ");

        Process(right);
        _builder.Append(')');
    }


    private void MultiOperandStringify(string name, IEnumerable<Expression> expressions)
    {
        _builder.Append($"({name}");

        foreach (var expression in expressions)
        {
            _builder.Append(' ');
            Process(expression);
        }

        _builder.Append(')');
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



    private void BeginStatement()
    {
        _builder.Append(Indent());
    }

    private void EndStatement()
    {
        _builder.Append(NewlineChar());
    }





    private void Process(Statement statement)
    {
        statement.Process(this);
    }




    public void ProcessExpression(ExpressionStatement statement)
    {
        BeginStatement();

        Process(statement.Expression);

        EndStatement();
    }




    public void ProcessDeclaration(DeclarationStatement statement)
    {
        BeginStatement();

        _builder.Append($"{statement.Type.Lexeme} {statement.Name.Lexeme} = ");
        Process(statement.Value);

        EndStatement();
    }




    public void ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        BeginStatement();

        _builder.Append($"{statement.ReturnType.Lexeme} {statement.Name.Lexeme}(");

        var parameters = statement.Parameters.ToArray();

        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];
            var atEnd = i + 1 >= parameters.Length;

            _builder.Append($"{parameter.Type.Lexeme} {parameter.Name.Lexeme}{(!atEnd ? ", " : "")}");
        }

        _builder.Append($") {NewlineChar()}");

        Process(statement.Body);

        EndStatement();
    }




    public void ProcessReturn(ReturnStatement statement)
    {
        BeginStatement();

        _builder.Append("return");

        if (statement.Expression is not null)
        {
            _builder.Append(' ');
            Process(statement.Expression);
        }

        EndStatement();
    }




    public void ProcessBlock(BlockStatement blockStatement)
    {
        if (IgnoreBlocks)
        {
            _builder.Append("block...");
            return;
        }

        BeginStatement();
        _builder.Append($"{{{NewlineChar()}");

        IncreaseIndent();

        foreach (var statement in blockStatement.Statements)
            Process(statement);

        DecreaseIndent();

        _builder.Append($"{Indent()}}}");
        EndStatement();
    }





    public void Process(Expression expression)
    {
        expression.Process(this);
    }




    public void ProcessLiteral(LiteralExpression expression)
    {
        _builder.Append(expression.Value.Lexeme);
    }




    public void ProcessBinary(BinaryExpression expression)
    {
        Stringify(expression.Operator.Lexeme, [expression.Left, expression.Right]);
    }


    public void ProcessGrouping(GroupingExpression expression)
    {
        Parenthesize(expression.Expression);
    }




    public void ProcessIdentifier(IdentifierExpression expression)
    {
        _builder.Append($"{(expression.GetAddress ? "&" : "$")}{expression.Identifier.Lexeme}");
    }


    public void ProcessAssignment(AssignmentExpression expression)
    {
        BinaryStringify("=", expression.Identifier, expression.Value);
    }




    public void ProcessCall(CallExpression expression)
    {
        Process(expression.Callee);
        _builder.Append('(');

        var arguments = expression.Arguments.ToArray();

        for (var i = 0; i < arguments.Length; i++)
        {
            var argument = arguments[i];
            var atEnd = i + 1 >= arguments.Length;

            Process(argument);
            _builder.Append(atEnd ? "" : ", ");
        }

        _builder.Append(')');
    }




    public void ProcessCast(CastExpression expression)
    {
        _builder.Append('(');
        Process(expression.Expression);
        _builder.Append($" {expression.Keyword.Lexeme} {expression.Type.Lexeme}");
        _builder.Append(')');
    }
}
