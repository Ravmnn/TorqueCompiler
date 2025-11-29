using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public class TorqueParser(IEnumerable<Token> tokens)
{
    public const PrimitiveType DefaultPrimitiveType = PrimitiveType.Int32;




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
            catch (LanguageException exception)
            {
                Torque.LogError(exception);
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
        Expect(TokenType.Equal, TorqueErrors.ExpectAssignmentOperator(Peek().Location));

        var value = Expression();

        ExpectEndOfStatement();

        return new DeclarationStatement(name, type, value);
    }




    private Statement FunctionDeclaration(Token returnType, Token name)
    {
        Expect(TokenType.ParenLeft, TorqueErrors.ExpectLeftParenAfterFunctionName(Peek().Location));
        var parameters = FunctionParameters();
        Expect(TokenType.ParenRight, TorqueErrors.ExpectRightParenBeforeReturnType(Peek().Location));

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
                Expect(TokenType.Colon, TorqueErrors.ExpectTypeSpecifier(Peek().Location));
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

            if (Torque.Failed) // something already went wrong, ignore
                return null;

            throw TorqueErrors.WrongKeywordPlacement(Peek().Location);


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

        var start = Expect(TokenType.CurlyBraceLeft, TorqueErrors.ExpectBlockStatement(Peek().Location));

        while (!AtEnd() && !Check(TokenType.CurlyBraceRight))
            if (Declaration() is { } declaration)
                block.Add(declaration);

        var end = Expect(TokenType.CurlyBraceRight, TorqueErrors.UnclosedBlockStatement(Peek().Location));

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

            if (expression is not IdentifierExpression identifier)
                throw TorqueErrors.ExpectIdentifier(@operator);

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

            Expect(TokenType.ParenRight, TorqueErrors.ExpectRightParenAfterArguments(Peek().Location));

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
            return new IdentifierExpression(ExpectIdentifier(), true);

        if (Match(TokenType.Identifier))
            return new IdentifierExpression(Previous());

        if (Match(TokenType.ParenLeft))
            return ParseGroupExpression();

        throw TorqueErrors.ExpectExpression(Peek().Location);
    }


    private Expression ParseLiteral()
    {
        var token = Previous();

        return new LiteralExpression(token, DefaultPrimitiveType);
    }


    private Expression ParseGroupExpression()
    {
        var leftParen = Previous();
        var expression = Expression();

        Expect(TokenType.ParenRight, TorqueErrors.ExpectExpression(leftParen.Location));

        return new GroupingExpression(expression);
    }




    private void Synchronize()
    {
        Advance();

        while (!AtEnd())
        {
            switch (Peek().Type)
            {
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




    private Token Expect(TokenType token, LanguageException exception)
    {
        if (Check(token))
            return Advance();

        throw exception;
    }


    private Token ExpectEndOfStatement()
        => Expect(TokenType.SemiColon, TorqueErrors.ExpectSemicolonAfterStatement(Previous().Location));


    private Token ExpectIdentifier()
        => Expect(TokenType.Identifier, TorqueErrors.ExpectIdentifier(Peek().Location));


    private Token ExpectTypeName()
        => Expect(TokenType.Type, TorqueErrors.ExpectTypeName(Peek().Location));




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
