using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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


    private void Reset()
    {
        _statements.Clear();
        _current = 0;
    }








    #region Statements

    private Statement? Declaration() => Peek().Type switch
    {
        TokenType.Type => GenericDeclaration(),

        _ => Statement()
    };


    private Statement GenericDeclaration()
    {
        var type = ParseTypeName();
        var name = ExpectIdentifier();

        if (Check(TokenType.LeftParen))
            return FunctionDeclaration(type, name);

        return VariableDeclaration(type, name);
    }




    private Statement VariableDeclaration(TypeName type, Token name)
    {
        Expect(TokenType.Equal, Diagnostic.ParserCatalog.ExpectAssignmentOperator);
        var value = Expression();
        ExpectEndOfStatement();

        return new DeclarationStatement(name, type, value);
    }




    private Statement FunctionDeclaration(TypeName returnType, Token name)
    {
        ExpectLeftParen();
        var parameters = FunctionParameters();
        ExpectRightParen();

        var body = Block() as BlockStatement;

        return new FunctionDeclarationStatement(returnType, name, parameters, body!);
    }


    private IEnumerable<FunctionParameterDeclaration> FunctionParameters()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(() =>
        {
            var type = ParseTypeName();
            var name = ExpectIdentifier();

            return new FunctionParameterDeclaration(name, type);
        });
    }




    private Statement? Statement()
    {
        switch (Peek().Type)
        {
        case TokenType.LeftCurlyBrace: return Block();
        case TokenType.KwReturn: return ReturnStatement();

        // some tokens only makes sense when together with another,
        // but parser exceptions may break that "together", leaving those
        // tokens without any processing. To avoid unnecessary error messages,
        // some tokens should be ignored:

        case TokenType.RightCurlyBrace:
            Advance();

            if (HasReports) // something already went wrong, ignore
                return null;

            ReportAndThrow(Diagnostic.ParserCatalog.WrongBlockPlacement);
            throw new UnreachableException();


        default: return ExpressionStatement();
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
        var start = Expect(TokenType.LeftCurlyBrace, Diagnostic.ParserCatalog.ExpectBlock);

        while (!AtEnd() && !Check(TokenType.RightCurlyBrace))
            if (Declaration() is { } declaration)
                block.Add(declaration);

        var end = Expect(TokenType.RightCurlyBrace, Diagnostic.ParserCatalog.UnclosedBlock);

        return new BlockStatement(start, end, block);
    }

    #endregion








    #region Expressions

    private Expression Expression()
        => Assignment();




    private Expression Assignment()
    {
        var expression = Term();

        if (Match(TokenType.Equal))
        {
            var @operator = Previous();
            var value = Assignment();
            expression = new AssignmentExpression(expression, @operator, value);
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
        var expression = PointerAccess();

        while (Match(TokenType.KwAs))
        {
            var keyword = Previous();
            var type = ParseTypeName();
            expression = new CastExpression(expression, keyword, type);
        }

        return expression;
    }




    private Expression PointerAccess()
    {
        if (Match(TokenType.Star))
        {
            var @operator = Previous();
            var pointer = PointerAccess();

            return new PointerAccessExpression(@operator, pointer);
        }

        return Unary();
    }




    private Expression Unary()
    {
        if (Match(TokenType.Minus, TokenType.Exclamation))
        {
            var @operator = Previous();
            var expression = Unary();

            return new UnaryExpression(@operator, expression);
        }

        return Call();
    }




    private Expression Call()
    {
        var expression = Primary();

        while (Match(TokenType.LeftParen))
        {
            var parenLeft = Previous();
            var arguments = Arguments();

            ExpectRightParen();

            expression = new CallExpression(parenLeft, expression, arguments);
        }

        return expression;
    }


    private Expression[] Arguments()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(Expression);
    }




    private Expression Primary()
    {
        if (PrimaryOrNull() is { } expression)
            return expression;

        ReportAndThrow(Diagnostic.ParserCatalog.ExpectExpression);
        throw new UnreachableException();
    }


    private Expression? PrimaryOrNull() => Peek().Type switch
    {
        _ when Match(TokenType.Value) => ParseLiteral(),
        _ when Match(TokenType.Ampersand) => new SymbolExpression(ExpectIdentifier(), true),
        _ when Match(TokenType.Identifier) => new SymbolExpression(Previous()),
        _ when Match(TokenType.LeftParen) => ParseGroupExpression(),

        _ => null
    };


    private Expression ParseLiteral()
        => new LiteralExpression(Previous());


    private Expression ParseGroupExpression()
    {
        var expression = Expression();
        ExpectRightParen();

        return new GroupingExpression(expression);
    }

    #endregion




    private TypeName ParseTypeName()
    {
        var baseType = ExpectTypeName();
        Token? pointerSpecifier = null;

        if (Match(TokenType.LeftParen))
            return ParseFunctionTypeName(baseType);

        if (Match(TokenType.Star))
            pointerSpecifier = Previous();

        return new TypeName(baseType, pointerSpecifier);
    }


    private TypeName ParseFunctionTypeName(Token baseType)
    {
        var parameters = ParseFunctionTypeNameParameters();
        ExpectRightParen();

        // TODO: use IEnumerable<T> instead of T[] if indexing doesn't matter
        return new FunctionTypeName(baseType, parameters);
    }


    private TypeName[] ParseFunctionTypeNameParameters()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(ParseTypeName);
    }




    #region Error handling

    private void Synchronize()
    {
        Advance();

        while (!AtEnd())
        {
            switch (Peek().Type)
            {
                case TokenType.Type:
                case TokenType.LeftCurlyBrace:
                case TokenType.RightCurlyBrace:
                case TokenType.KwReturn:
                    return;
            }

            Advance();
        }
    }




    [DoesNotReturn]
    public override void ReportAndThrow(Diagnostic.ParserCatalog item, object[]? arguments = null, TokenLocation? location = null)
        => base.ReportAndThrow(item, arguments, location ?? Peek());




    private Token Expect(TokenType token, Diagnostic.ParserCatalog item, TokenLocation? location = null)
    {
        if (Check(token))
            return Advance();

        ReportAndThrow(item, location: location);
        throw new UnreachableException();
    }


    private Token ExpectEndOfStatement()
        => Expect(TokenType.SemiColon, Diagnostic.ParserCatalog.ExpectSemicolonAfterStatement);


    private Token ExpectIdentifier()
        => Expect(TokenType.Identifier, Diagnostic.ParserCatalog.ExpectIdentifier);


    private Token ExpectTypeName()
        => Expect(TokenType.Type, Diagnostic.ParserCatalog.ExpectTypeName);


    private Token ExpectLeftParen()
        => Expect(TokenType.LeftParen, Diagnostic.ParserCatalog.ExpectLeftParen);

    private Token ExpectRightParen()
        => Expect(TokenType.RightParen, Diagnostic.ParserCatalog.ExpectRightParen);

    #endregion




    #region Navigation methods

    private void DoWhileComma(Action action)
    {
        do
            action();
        while (Match(TokenType.Comma));
    }


    private T[] DoWhileComma<T>(Func<T> func)
    {
        var list = new List<T>();

        do
            list.Add(func());
        while (Match(TokenType.Comma));

        return list.ToArray();
    }




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

    #endregion
}
