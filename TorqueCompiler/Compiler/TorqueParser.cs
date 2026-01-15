using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler;




public class TorqueParser(IReadOnlyList<Token> tokens) : DiagnosticReporter<ParserCatalog>
{
    private readonly List<Statement> _statements = [];
    private int _current;


    public IReadOnlyList<Token> Tokens { get; } = tokens;




    public IReadOnlyList<Statement> Parse()
    {
        Reset();

        while (!AtEnd())
            ParseDeclarationOrSynchronize();

        return _statements;
    }


    private void ParseDeclarationOrSynchronize()
    {
        try
        {
            ParseDeclaration();
        }
        catch (DiagnosticException)
        {
            Synchronize();
        }
    }


    private void ParseDeclaration()
    {
        if (Declaration() is { } declaration)
            _statements.Add(declaration);
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
        var symbol = new SymbolSyntax(ExpectIdentifier());

        if (Check(TokenType.LeftParen))
            return FunctionDeclaration(type, symbol);

        return VariableDeclaration(type, symbol);
    }




    private Statement VariableDeclaration(TypeSyntax type, SymbolSyntax name)
    {
        if (Match(TokenType.SemiColon))
            return new SugarDefaultDeclarationStatement(type, name);

        Expect(TokenType.Equal, ParserCatalog.ExpectAssignmentOperator);
        var value = Expression();
        ExpectEndOfStatement();

        return new DeclarationStatement(type, name, value);
    }




    private Statement FunctionDeclaration(TypeSyntax returnType, SymbolSyntax name)
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
            var symbol = new SymbolSyntax(ExpectIdentifier());

            return new FunctionParameterDeclaration(symbol, type);
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

            ReportAndThrow(ParserCatalog.UnexpectedToken);
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
        var start = Expect(TokenType.LeftCurlyBracket, ParserCatalog.ExpectBlock);

        while (!AtEnd() && !Check(TokenType.RightCurlyBracket))
            if (Declaration() is { } declaration)
                block.Add(declaration);

        Expect(TokenType.RightCurlyBracket, ParserCatalog.UnclosedBlock);

        return new BlockStatement(block, start.Location);
    }




    private Statement If()
    {
        var keyword = Advance();

        ExpectLeftParen();
        var condition = Expression();
        var rightParen = ExpectRightParen();

        var thenStatement = Statement()!;
        var elseStatement = ElseOrNull();

        var location = new Span(keyword, rightParen);
        return new IfStatement(condition, thenStatement, elseStatement, location);
    }


    private Statement? ElseOrNull()
    {
        if (Match(TokenType.KwElse))
            return Statement();

        return null;
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
            var type = ParseTypeName();
            var location = new Span(expression.Location, type.BaseType.TypeSymbol.Location);

            expression = new CastExpression(expression, type, location);
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
            var location = new Span(expression.Location, rightSquareBracket);

            expression = new IndexingExpression(expression, index, location);
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
            var location = new Span(expression.Location, parenRight);

            expression = new CallExpression(expression, arguments, location);
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

        ReportAndThrow(ParserCatalog.ExpectExpression);
        throw new UnreachableException();
    }


    private Expression? PrimaryOrNull() => Peek().Type switch
    {
        _ when CurrentIsLiteral() => ParseLiteral(),

        _ when Match(TokenType.Identifier) => new SymbolExpression(new SymbolSyntax(Previous())),
        _ when Match(TokenType.LeftParen) => ParseGroupExpression(),

        _ when Check(TokenType.Type) => TryParseArray(),

        _ when Match(TokenType.KwDefault) => ParseDefault(),

        _ => null
    };


    private bool CurrentIsLiteral()
        => Match(TokenType.IntegerValue, TokenType.FloatValue, TokenType.BoolValue, TokenType.CharValue, TokenType.StringValue);




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
        var size = (ulong)ExpectLiteralInteger().Value!; // TODO: array length should be able to be inferred from the initialization list
        var rightSquareBracket = ExpectRightSquareBracket();

        var expressions = GetOptionalArrayInitializationExpressions();
        var location = new Span(type.BaseType.TypeSymbol.Location, rightSquareBracket);

        return new ArrayExpression(type, size, expressions, location);
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

    private TypeSyntax ParseTypeName()
        => ParseTypeName(new Dictionary<TokenType, Func<TypeSyntax, TypeSyntax>>
        {
            { TokenType.Star, ParsePointerTypeName },
            { TokenType.LeftSquareBracket, ParseArrayTypeName },
            { TokenType.LeftParen, ParseFunctionTypeName }
        });


    private TypeSyntax ParseTypeName(Dictionary<TokenType, Func<TypeSyntax, TypeSyntax>> processors)
    {
        var typeNameSymbol = new SymbolSyntax(ExpectTypeName());
        TypeSyntax type = new BaseTypeSyntax(typeNameSymbol);

        while (true)
            if (!ModifyCurrentTypeNameFromProcessors(ref type, processors))
                break;

        return type;
    }


    private bool ModifyCurrentTypeNameFromProcessors(ref TypeSyntax type, Dictionary<TokenType, Func<TypeSyntax, TypeSyntax>> processors)
    {
        foreach (var (token, processor) in processors)
            if (Match(token))
            {
                type = processor(type);
                return true;
            }

        return false;
    }


    private TypeSyntax ParsePointerTypeName(TypeSyntax type)
        => new PointerTypeSyntax(type);


    private TypeSyntax ParseArrayTypeName(TypeSyntax type)
    {
        ExpectRightSquareBracket();
        return new PointerTypeSyntax(type);
    }


    private TypeSyntax ParseFunctionTypeName(TypeSyntax type)
    {
        var parameters = ParseFunctionTypeNameParameters();
        ExpectRightParen();

        return new FunctionTypeSyntax(type, parameters);
    }


    private IReadOnlyList<TypeSyntax> ParseFunctionTypeNameParameters()
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
    public override void ReportAndThrow(ParserCatalog item, IReadOnlyList<object>? arguments = null, Span? location = null)
        => base.ReportAndThrow(item, arguments, location ?? Peek().Location);




    private Token Expect(TokenType token, ParserCatalog item, Span? location = null)
    {
        if (Check(token))
            return Advance();

        ReportAndThrow(item, location: location);
        throw new UnreachableException();
    }


    private Token ExpectEndOfStatement()
        => Expect(TokenType.SemiColon, ParserCatalog.ExpectSemicolonAfterStatement);


    private Token ExpectIdentifier()
        => Expect(TokenType.Identifier, ParserCatalog.ExpectIdentifier);


    private Token ExpectTypeName()
        => Expect(TokenType.Type, ParserCatalog.ExpectTypeName);


    private Token ExpectLeftParen()
        => Expect(TokenType.LeftParen, ParserCatalog.ExpectLeftParen);

    private Token ExpectRightParen()
        => Expect(TokenType.RightParen, ParserCatalog.ExpectRightParen);


    private Token ExpectLeftSquareBracket()
        => Expect(TokenType.LeftSquareBracket, ParserCatalog.ExpectLeftSquareBracket);

    private Token ExpectRightSquareBracket()
        => Expect(TokenType.RightSquareBracket, ParserCatalog.ExpectRightSquareBracket);


    private Token ExpectLeftCurlyBracket()
        => Expect(TokenType.LeftCurlyBracket, ParserCatalog.ExpectLeftCurlyBracket);

    private Token ExpectRightCurlyBracket()
        => Expect(TokenType.RightCurlyBracket, ParserCatalog.ExpectRightCurlyBracket);


    private Token ExpectLiteralInteger()
        => Expect(TokenType.IntegerValue, ParserCatalog.ExpectLiteralInteger);

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
