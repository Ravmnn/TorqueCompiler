using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueParser(IEnumerable<Token> tokens) : DiagnosticReporter<Diagnostic.ParserCatalog>
{
    private readonly List<Statement> _statements = [];
    private uint _current;


    public Token[] Tokens { get; set; } = tokens.ToArray();




    public IEnumerable<Statement> Parse()
    {
        Reset();

        while (!AtEnd())
        {
            try
            {
                if (Declaration() is { } declaration)
                    _statements.Add(declaration);
            }
            catch (DiagnosticException)
            {
                Synchronize();
            }
        }

        return _statements;
    }




    private Statement? Declaration()
    {
        switch (Peek().Type)
        {
        case TokenType.Type: {
            var type = ExpectTypeName();
            var name = ExpectIdentifier();

            if (Check(TokenType.ParenLeft))
                return FunctionDeclaration(type, name);

            return VariableDeclaration(type, name);
        }

        default:
            return Statement();
        }
    }




    private Statement VariableDeclaration(Token type, Token name)
    {
        Expect(TokenType.Equal, Diagnostic.ParserCatalog.ExpectAssignmentOperator);

        var value = Expression();

        ExpectEndOfStatement();

        return new DeclarationStatement(name, type, value);
    }




    private Statement FunctionDeclaration(Token returnType, Token name)
    {
        Expect(TokenType.ParenLeft, Diagnostic.ParserCatalog.ExpectLeftParenAfterFunctionName);
        var parameters = FunctionParameters();
        Expect(TokenType.ParenRight, Diagnostic.ParserCatalog.ExpectRightParenBeforeReturnType);

        var body = Block() as BlockStatement;

        return new FunctionDeclarationStatement(name, returnType, parameters, body!);
    }


    private IEnumerable<FunctionParameterDeclaration> FunctionParameters()
    {
        var parameters = new List<FunctionParameterDeclaration>();

        if (Check(TokenType.Identifier))
            do
            {
                var name = ExpectIdentifier();
                Expect(TokenType.Colon, Diagnostic.ParserCatalog.ExpectTypeSpecifier);
                var type = ExpectTypeName();

                parameters.Add(new FunctionParameterDeclaration(name, type));
            }
            while (Match(TokenType.Comma));

        return parameters;
    }




    private Statement? Statement()
    {
        switch (Peek().Type)
        {
        case TokenType.CurlyBraceLeft: return Block();
        case TokenType.KwReturn: return ReturnStatement();

        // some tokens only make sense when together with another,
        // but parser exceptions may break that "together", leaving those
        // tokens without any processing. To avoid unnecessary error messages,
        // some tokens should be ignored:

        case TokenType.CurlyBraceRight:
            Advance();

            if (HasReports) // something already went wrong, ignore
                return null;

            ReportAndThrow(Diagnostic.ParserCatalog.WrongBlockPlacement);
            throw new UnreachableException();


        default:
            return ExpressionStatement();
        }
    }




    private Statement ExpressionStatement()
    {
        var expression = Expression();
        ExpectEndOfStatement();

        return new ExpressionStatement(expression);
    }




    private Statement ReturnStatement()
    {
        var keyword = Advance();
        Expression? expression = null;

        if (!Check(TokenType.SemiColon))
            expression = Expression();

        ExpectEndOfStatement();

        return new ReturnStatement(keyword, expression);
    }




    private Statement Block()
    {
        var block = new List<Statement>();

        var start = Expect(TokenType.CurlyBraceLeft, Diagnostic.ParserCatalog.ExpectBlock);

        while (!AtEnd() && !Check(TokenType.CurlyBraceRight))
            if (Declaration() is { } declaration)
                block.Add(declaration);

        var end = Expect(TokenType.CurlyBraceRight, Diagnostic.ParserCatalog.UnclosedBlock);

        return new BlockStatement(start, end, block);
    }




    private Expression Expression()
    {
        return Assignment();
    }




    private Expression Assignment()
    {
        var expression = Term();

        if (Match(TokenType.Equal))
        {
            var @operator = Previous();
            var value = Assignment();

            if (expression is not SymbolExpression { GetAddress: false } identifier)
            {
                ReportAndThrow(Diagnostic.ParserCatalog.ExpectIdentifier);
                throw new UnreachableException();
            }

            expression = new AssignmentExpression(identifier, @operator, value);
        }

        return expression;
    }




    private Expression Term()
    {
        var expression = Factor();

        while (Match(TokenType.Plus, TokenType.Minus))
        {
            var @operator = Previous();
            var right = Factor();
            expression = new BinaryExpression(expression, @operator, right);
        }

        return expression;
    }




    private Expression Factor()
    {
        var expression = Cast();

        while (Match(TokenType.Star, TokenType.Slash))
        {
            var @operator = Previous();
            var right = Cast();
            expression = new BinaryExpression(expression, @operator, right);
        }

        return expression;
    }




    private Expression Cast()
    {
        var expression = Call();

        while (Match(TokenType.KwAs))
        {
            var keyword = Previous();
            var type = ExpectTypeName();
            expression = new CastExpression(keyword, expression, type);
        }

        return expression;
    }




    private Expression Call()
    {
        var expression = Primary();

        while (Match(TokenType.ParenLeft))
        {
            var parenLeft = Previous();
            var arguments = new List<Expression>();

            if (!Check(TokenType.ParenRight))
                arguments = Arguments().ToList();

            Expect(TokenType.ParenRight, Diagnostic.ParserCatalog.ExpectRightParenAfterArguments);

            expression = new CallExpression(parenLeft, expression, arguments);
        }

        return expression;
    }


    private IEnumerable<Expression> Arguments()
    {
        var expressions = new List<Expression>();

        do
            expressions.Add(Expression());
        while (Match(TokenType.Comma));


        return expressions;
    }




    private Expression Primary()
    {
        if (Match(TokenType.Value))
            return ParseLiteral();

        if (Match(TokenType.Ampersand))
            return new SymbolExpression(ExpectIdentifier(), true);

        if (Match(TokenType.Identifier))
            return new SymbolExpression(Previous());

        if (Match(TokenType.ParenLeft))
            return ParseGroupExpression();

        ReportAndThrow(Diagnostic.ParserCatalog.ExpectExpression);
        throw new UnreachableException();
    }


    private Expression ParseLiteral()
    {
        return new LiteralExpression(Previous());
    }


    private Expression ParseGroupExpression()
    {
        var leftParen = Previous();
        var expression = Expression();

        Expect(TokenType.ParenRight, Diagnostic.ParserCatalog.ExpectExpression, leftParen);

        return new GroupingExpression(expression);
    }




    private void Synchronize()
    {
        Advance();

        while (!AtEnd())
        {
            switch (Peek().Type)
            {
                case TokenType.Type:
                case TokenType.CurlyBraceLeft:
                case TokenType.CurlyBraceRight:
                case TokenType.KwReturn:
                    return;
            }

            Advance();
        }
    }




    private void Reset()
    {
        _statements.Clear();
        _current = 0;
    }




    private Token Expect(TokenType token, Diagnostic.ParserCatalog item, TokenLocation? location = null)
    {
        if (Check(token))
            return Advance();

        ReportAndThrow(item, location: location ?? Peek().Location);
        throw new UnreachableException();
    }


    private Token ExpectEndOfStatement()
        => Expect(TokenType.SemiColon, Diagnostic.ParserCatalog.ExpectSemicolonAfterStatement);


    private Token ExpectIdentifier()
        => Expect(TokenType.Identifier, Diagnostic.ParserCatalog.ExpectIdentifier);


    private Token ExpectTypeName()
        => Expect(TokenType.Type, Diagnostic.ParserCatalog.ExpectTypeName);




    private bool Match(params IEnumerable<TokenType> tokens)
    {
        foreach (var token in tokens)
        {
            if (!Check(token))
                continue;

            Advance();
            return true;
        }

        return false;
    }


    private bool Check(TokenType token)
    {
        if (AtEnd())
            return false;

        return Peek().Type == token;
    }


    private Token Advance()
    {
        if (AtEnd())
            return Previous();

        return Tokens[_current++];
    }


    private Token Peek()
        => !AtEnd() ? Tokens[_current] : Previous();


    private Token Previous(int amount = 1)
    {
        if (_current <= amount - 1)
            return Peek();

        return Tokens[_current - amount];
    }


    private bool AtEnd()
        => _current >= Tokens.Length;
}
