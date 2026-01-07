using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueParser(IReadOnlyList<Token> tokens) : DiagnosticReporter<Diagnostic.ParserCatalog>
{
    private readonly List<Statement> _statements = [];
    private int _current;


    public IReadOnlyList<Token> Tokens { get; } = tokens;




    public IReadOnlyList<Statement> Parse()
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
        var name = new SymbolSyntax(ExpectIdentifier());

        if (Check(TokenType.LeftParen))
            return FunctionDeclaration(type, name);

        return VariableDeclaration(type, name);
    }




    private Statement VariableDeclaration(TypeName type, SymbolSyntax name)
    {
        if (Match(TokenType.SemiColon))
            return new SugarDefaultDeclarationStatement(type, name);

        Expect(TokenType.Equal, Diagnostic.ParserCatalog.ExpectAssignmentOperator);
        var value = Expression();
        ExpectEndOfStatement();

        return new DeclarationStatement(type, name, value);
    }




    private Statement FunctionDeclaration(TypeName returnType, SymbolSyntax name)
    {
        ExpectLeftParen();
        var parameters = FunctionParameters();
        ExpectRightParen();

        var body = (Block() as BlockStatement)!;

        return new FunctionDeclarationStatement(returnType, name, parameters, body);
    }


    private IReadOnlyList<FunctionParameterDeclaration> FunctionParameters()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(() =>
        {
            var type = ParseTypeName();
            var name = new SymbolSyntax(ExpectIdentifier());

            return new FunctionParameterDeclaration(name, type);
        });
    }




    private Statement? Statement()
    {
        switch (Peek().Type)
        {
        case TokenType.KwReturn: return ReturnStatement();
        case TokenType.LeftCurlyBracket: return Block();
        case TokenType.KwIf: return If();

        // some tokens only makes sense when together with another,
        // but parser exceptions may break that "together", leaving those
        // tokens without any processing. To avoid unnecessary error messages,
        // some tokens should be ignored:

        case TokenType.RightCurlyBracket:
            Advance();

            if (HasReports) // something already went wrong, ignore
                return null;

            ReportAndThrow(Diagnostic.ParserCatalog.UnexpectedToken);
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
        var start = Expect(TokenType.LeftCurlyBracket, Diagnostic.ParserCatalog.ExpectBlock);

        while (!AtEnd() && !Check(TokenType.RightCurlyBracket))
            if (Declaration() is { } declaration)
                block.Add(declaration);

        Expect(TokenType.RightCurlyBracket, Diagnostic.ParserCatalog.UnclosedBlock);

        return new BlockStatement(block, start.Location);
    }




    private Statement If()
    {
        var keyword = Advance();

        ExpectLeftParen();
        var condition = Expression();
        var rightParen = ExpectRightParen();

        var thenStatement = Statement()!;
        Statement? elseStatement = null;

        if (Match(TokenType.KwElse))
            elseStatement = Statement();

        return new IfStatement(condition, thenStatement, elseStatement, new Span(keyword, rightParen));
    }

    #endregion








    #region Expressions

    private Expression Expression()
        => Assignment();




    private Expression Assignment()
        => ParseRightAssociativeBinaryLayoutExpression<AssignmentExpression>(Logic, TokenType.Equal);




    private Expression Logic()
        => ParseLeftAssociativeBinaryLayoutExpression<LogicExpression>(Equality, TokenType.LogicAnd, TokenType.LogicOr);




    private Expression Equality()
        => ParseLeftAssociativeBinaryLayoutExpression<EqualityExpression>(Comparison, TokenType.Equality, TokenType.Inequality);




    private Expression Comparison()
        => ParseLeftAssociativeBinaryLayoutExpression<ComparisonExpression>(Term, TokenType.GreaterThan, TokenType.LessThan,
            TokenType.GreaterThanOrEqual, TokenType.LessThanOrEqual);




    private Expression Term()
        => ParseLeftAssociativeBinaryLayoutExpression<BinaryExpression>(Factor, TokenType.Plus, TokenType.Minus);




    private Expression Factor()
        => ParseLeftAssociativeBinaryLayoutExpression<BinaryExpression>(Cast, TokenType.Star, TokenType.Slash);




    private Expression Cast()
    {
        var expression = PointerAccess();

        while (Match(TokenType.KwAs))
        {
            var keyword = Previous();
            var type = ParseTypeName();
            expression = new CastExpression(expression, type, new Span(expression.Location, type.Base.TypeToken));
        }

        return expression;
    }




    private Expression PointerAccess()
        => ParseRightAssociativeUnaryLayoutExpression<PointerAccessExpression>(Unary, TokenType.Star);




    private Expression Unary()
        => ParseRightAssociativeUnaryLayoutExpression<UnaryExpression>(Address, TokenType.Exclamation, TokenType.Minus);




    private Expression Address()
        => ParseRightAssociativeUnaryLayoutExpression<AddressExpression>(Indexing, TokenType.Ampersand);




    private Expression Indexing()
    {
        var expression = Call();

        while (Match(TokenType.LeftSquareBracket))
        {
            var index = Expression();
            var rightSquareBracket = ExpectRightSquareBracket();

            expression = new IndexingExpression(expression, index, new Span(expression.Location, rightSquareBracket));
        }

        return expression;
    }




    private Expression Call()
    {
        var expression = Primary();

        while (Match(TokenType.LeftParen))
        {
            var arguments = Arguments();
            var parenRight = ExpectRightParen();

            expression = new CallExpression(expression, arguments, new Span(expression.Location, parenRight));
        }

        return expression;
    }


    private IReadOnlyList<Expression> Arguments()
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
        _ when Match(TokenType.IntegerValue, TokenType.FloatValue, TokenType.BoolValue, TokenType.CharValue) => ParseLiteral(),

        _ when Match(TokenType.Identifier) => new SymbolExpression(new SymbolSyntax(Previous())),
        _ when Match(TokenType.LeftParen) => ParseGroupExpression(),

        _ when Check(TokenType.Type) => TryParseArray(),

        _ when Match(TokenType.KwDefault) => ParseDefault(),

        _ => null
    };




    private Expression ParseLiteral()
    {
        var literal = Previous();
        return new LiteralExpression(literal.Value!, literal.Location);
    }


    private Expression ParseGroupExpression()
    {
        var leftParen = Previous();
        var expression = Expression();
        var rightParen = ExpectRightParen();

        return new GroupingExpression(expression, new Span(leftParen, rightParen));
    }




    private Expression? TryParseArray()
    {
        var type = ParseTypeName();

        if (!Match(TokenType.KwArray))
            return null;

        ExpectLeftSquareBracket();
        var size = (ulong)ExpectLiteralInteger().Value!; // TODO: size should be able to be inferred from the initialization list
        var rightSquareBracket = ExpectRightSquareBracket();

        var expressions = GetOptionalArrayInitializationExpressions();

        return new ArrayExpression(type, size, expressions, new Span(type.Base.TypeToken, rightSquareBracket));
    }


    private IReadOnlyList<Expression>? GetOptionalArrayInitializationExpressions()
    {
        if (!Match(TokenType.LeftCurlyBracket))
            return null;

        var expressions = DoWhileComma(Expression);
        ExpectRightCurlyBracket();

        return expressions;
    }




    private Expression ParseDefault()
    {
        var keyword = Previous();
        var typeName = ParseTypeName();
        var rightParen = ExpectRightParen();

        return new DefaultExpression(typeName, new Span(keyword, rightParen));
    }

    #endregion




    #region Parse Methods

    private Expression ParseLeftAssociativeBinaryLayoutExpression<T>(Func<Expression> predecessor, params IReadOnlyList<TokenType> operators)
        where T : BinaryLayoutExpression, IBinaryLayoutExpressionFactory
        => ParseAssociativeBinaryLayoutExpression<T>(predecessor, false, operators);


    private Expression ParseRightAssociativeBinaryLayoutExpression<T>(Func<Expression> predecessor, params IReadOnlyList<TokenType> operators)
        where T : BinaryLayoutExpression, IBinaryLayoutExpressionFactory
        => ParseAssociativeBinaryLayoutExpression<T>(predecessor, true, operators);




    private Expression ParseAssociativeBinaryLayoutExpression<T>(Func<Expression> predecessor, bool rightAssociative = false,
        params IReadOnlyList<TokenType> operators) where T : BinaryLayoutExpression, IBinaryLayoutExpressionFactory
    {
        var expression = predecessor();

        while (Match(operators))
        {
            var @operator = Previous();
            var right = rightAssociative ? ParseAssociativeBinaryLayoutExpression<T>(predecessor, rightAssociative, operators) : predecessor();
            expression = T.Create(expression, right, @operator.Type, new Span(expression.Location, right.Location));
        }

        return expression;
    }




    private Expression ParseRightAssociativeUnaryLayoutExpression<T>(Func<Expression> predecessor, params IReadOnlyList<TokenType> operators)
        where T : UnaryLayoutExpression, IUnaryLayoutExpressionFactory
    {
        if (Match(operators))
        {
            var @operator = Previous();
            var expression = ParseRightAssociativeUnaryLayoutExpression<T>(predecessor, operators);

            return T.Create(expression, @operator.Type, new Span(@operator, expression.Location));
        }

        return predecessor();
    }

    #endregion




    #region Type Name

    private TypeName ParseTypeName()
        => ParseTypeName(new Dictionary<TokenType, Func<TypeName, TypeName>>
        {
            { TokenType.Star, ParsePointerTypeName },
            { TokenType.LeftSquareBracket, ParseArrayTypeName },
            { TokenType.LeftParen, ParseFunctionTypeName }
        });


    private TypeName ParseTypeName(Dictionary<TokenType, Func<TypeName, TypeName>> processors)
    {
        TypeName type = new BaseTypeName(ExpectTypeName());

        while (true)
        {
            var shouldContinue = false;

            foreach (var (token, processor) in processors)
                if (Match(token))
                {
                    type = processor(type);
                    shouldContinue = true;
                    break;
                }

            if (!shouldContinue)
                break;
        }

        return type;
    }


    private TypeName ParsePointerTypeName(TypeName type)
        => new PointerTypeName(type, Previous());


    private TypeName ParseArrayTypeName(TypeName type)
    {
        ExpectRightSquareBracket();
        return new PointerTypeName(type);
    }


    private TypeName ParseFunctionTypeName(TypeName type)
    {
        var parameters = ParseFunctionTypeNameParameters();
        ExpectRightParen();

        return new FunctionTypeName(type, parameters);
    }


    private IReadOnlyList<TypeName> ParseFunctionTypeNameParameters()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(ParseTypeName);
    }

    #endregion




    #region Diagnostic Reporting

    private void Synchronize()
    {
        Advance();

        while (!AtEnd())
        {
            switch (Peek().Type)
            {
                case TokenType.SemiColon:
                case TokenType.KwReturn:
                    Advance();
                    return;
            }

            Advance();
        }
    }




    [DoesNotReturn]
    public override void ReportAndThrow(Diagnostic.ParserCatalog item, IReadOnlyList<object>? arguments = null, Span? location = null)
        => base.ReportAndThrow(item, arguments, location ?? Peek().Location);




    private Token Expect(TokenType token, Diagnostic.ParserCatalog item, Span? location = null)
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


    private Token ExpectLeftSquareBracket()
        => Expect(TokenType.LeftSquareBracket, Diagnostic.ParserCatalog.ExpectLeftSquareBracket);

    private Token ExpectRightSquareBracket()
        => Expect(TokenType.RightSquareBracket, Diagnostic.ParserCatalog.ExpectRightSquareBracket);


    private Token ExpectLeftCurlyBracket()
        => Expect(TokenType.LeftCurlyBracket, Diagnostic.ParserCatalog.ExpectLeftCurlyBracket);

    private Token ExpectRightCurlyBracket()
        => Expect(TokenType.RightCurlyBracket, Diagnostic.ParserCatalog.ExpectRightCurlyBracket);


    private Token ExpectLiteralInteger()
        => Expect(TokenType.IntegerValue, Diagnostic.ParserCatalog.ExpectLiteralInteger);

    #endregion




    #region Navigation methods

    private void DoWhileComma(Action action)
    {
        do
            action();
        while (Match(TokenType.Comma));
    }


    private IReadOnlyList<T> DoWhileComma<T>(Func<T> func)
    {
        var list = new List<T>();

        do
            list.Add(func());
        while (Match(TokenType.Comma));

        return list.ToArray();
    }




    private bool Match(params IReadOnlyList<TokenType> tokens)
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
        => _current >= Tokens.Count;

    #endregion
}
