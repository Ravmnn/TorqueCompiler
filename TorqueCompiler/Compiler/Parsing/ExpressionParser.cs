using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.Diagnostics.Catalogs;
using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.Parsing;




public partial class Parser
{
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
            var type = TryParseTypeSyntax()!;
            var location = new Span(expression.Location, type.BaseType.TypeSymbol.Location);

            expression = new CastExpression(expression, type, location);
        }

        return expression;
    }




    private Expression PointerAccess()
        => ParseRightAssociativeUnaryLayoutExpression<PointerAccessExpression>(PreFix, TokenType.Star);




    private Expression PreFix()
        => ParseRightAssociativeUnaryLayoutExpression<PreFixExpression>(Unary, TokenType.Increment, TokenType.Decrement);




    private Expression Unary()
        => ParseRightAssociativeUnaryLayoutExpression<UnaryExpression>(Address, TokenType.Exclamation, TokenType.Minus);




    private Expression Address()
        => ParseRightAssociativeUnaryLayoutExpression<AddressExpression>(PostFix, TokenType.Ampersand);




    private Expression PostFix()
    {
        var expression = Primary();

        while (true)
        {
            if (Match(TokenType.LeftParen))
                expression = PostFixCall(expression);

            else if (Match(TokenType.LeftSquareBracket))
                expression = PostFixIndexing(expression);

            else if (Match(TokenType.Dot, TokenType.Arrow))
                expression = PostFixMemberAccess(expression);

            else if (Match(TokenType.Decrement, TokenType.Increment))
                expression = PostFixIncrDecr(expression);

            else
                break;
        }

        return expression;
    }


    private Expression PostFixCall(Expression expression)
    {
        var arguments = Arguments();
        var parenRight = Reporter.ExpectRightParen();
        var location = new Span(expression.Location, parenRight);

        return new CallExpression(expression, arguments, location);
    }


    private IReadOnlyList<Expression> Arguments()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(Expression);
    }


    private Expression PostFixIndexing(Expression expression)
    {
        var index = Expression();
        var rightSquareBracket = Reporter.ExpectRightSquareBracket();
        var location = new Span(expression.Location, rightSquareBracket);

        return new IndexingExpression(expression, index, location);
    }


    private Expression PostFixMemberAccess(Expression expression)
    {
        var isArrow = Iterator.Previous().Type == TokenType.Arrow;
        var member = Reporter.ExpectSymbol();
        var location = new Span(expression.Location, member.Location);

        return isArrow ? new SugarArrowExpression(expression, member, location)
                                : new MemberAccessExpression(expression, member, location);
    }


    private Expression PostFixIncrDecr(Expression expression)
    {
        var @operator = Iterator.Previous();
        var location = new Span(expression.Location, @operator.Location);

        return new PostFixExpression(expression, @operator.Type, location);
    }




    private Expression Primary()
    {
        if (ArrayOrPrimaryOrNull() is { } expression)
            return expression;

        Reporter.ReportAndThrow(ParserCatalog.ExpectExpression);
        throw new UnreachableException();
    }


    private Expression? ArrayOrPrimaryOrNull()
    {
        if (Check(TokenType.Identifier) && TryParseArrayAndRegressIfFail() is { } array)
            return array;

        return PrimaryOrNull();
    }


    private Expression? PrimaryOrNull() => Iterator.Peek().Type switch
    {
        _ when CurrentIsLiteral() => ParseLiteral(),

        _ when Match(TokenType.Identifier) => ParseIdentifier(),
        _ when Match(TokenType.LeftParen) => ParseGroupExpression(),

        _ when Match(TokenType.KwDefault) => ParseDefault(),
        _ when Match(TokenType.KwNullptr) => ParseNullptr(),

        _ when Match(TokenType.KwNew) => ParseStructExpression(),

        _ => null
    };


    private bool CurrentIsLiteral()
        => Match(TokenType.IntegerValue, TokenType.FloatValue, TokenType.BoolValue, TokenType.CharValue, TokenType.StringValue);




    private Expression ParseLiteral()
    {
        var literal = Iterator.Previous();
        return new LiteralExpression(literal.Value!, literal.Location);
    }




    private SymbolExpression ParseIdentifier()
        => new SymbolExpression(new SymbolSyntax(Iterator.Previous()));




    private Expression ParseGroupExpression()
    {
        var leftParen = Iterator.Previous();
        var expression = Expression();
        var rightParen = Reporter.ExpectRightParen();

        return new GroupingExpression(expression, new Span(leftParen, rightParen));
    }




    private Expression? TryParseArrayAndRegressIfFail()
    {
        var current = Current;
        var array = TryParseArray();

        if (array is null)
            Current = current;

        return array;
    }


    private Expression? TryParseArray()
    {
        var type = TryParseTypeSyntax()!;

        if (!Match(TokenType.KwArray))
            return null;

        Reporter.ExpectLeftSquareBracket();

        var array = Match(TokenType.RightSquareBracket)
            ? ArrayWithImplicitSizeAndRequiredInitializationList()
            : ArrayWithExplicitSizeAndOptionalInitializationList();

        var location = new Span(type.BaseType.TypeSymbol.Location, Iterator.Previous());
        return new ArrayExpression(type, array.length, array.initializationList, location);
    }


    private (IReadOnlyList<Expression>? initializationList, ulong length) ArrayWithImplicitSizeAndRequiredInitializationList()
    {
        var initializationList = ExpectArrayInitializationList();
        var length = (ulong)initializationList.Count;

        return (initializationList, length);
    }


    private (IReadOnlyList<Expression>? initializationList, ulong length) ArrayWithExplicitSizeAndOptionalInitializationList()
    {
        var length = (ulong)Reporter.ExpectLiteralInteger().Value!;
        Reporter.ExpectRightSquareBracket();
        var initializationList = OptionalExpectArrayInitializationList();

        return (initializationList, length);
    }


    private IReadOnlyList<Expression>? OptionalExpectArrayInitializationList()
    {
        if (!Check(TokenType.LeftCurlyBracket))
            return null;

        return ExpectArrayInitializationList();
    }


    private IReadOnlyList<Expression> ExpectArrayInitializationList()
    {
        Reporter.ExpectLeftCurlyBracket();
        var expressions = DoWhileComma(Expression);
        Reporter.ExpectRightCurlyBracket();

        return expressions;
    }




    private Expression ParseDefault()
    {
        var keyword = Iterator.Previous();
        Reporter.ExpectLeftParen();
        var typeName = TryParseTypeSyntax()!;
        var rightParen = Reporter.ExpectRightParen();

        return new DefaultExpression(typeName, new Span(keyword, rightParen));
    }




    private Expression ParseNullptr()
        => new SugarNullptrExpression(Iterator.Previous());




    private Expression ParseStructExpression()
    {
        var keyword = Iterator.Previous();
        var symbol = Reporter.ExpectSymbol();

        Reporter.ExpectLeftCurlyBracket();
        var memberInitializations = ParseStructMembersInitialization();
        Reporter.ExpectRightCurlyBracket();

        var location = new Span(keyword, symbol.Location);
        return new StructExpression(symbol, memberInitializations, location);
    }


    private IReadOnlyList<StructMemberInitialization> ParseStructMembersInitialization() => DoWhileComma(() =>
    {
        var member = Reporter.ExpectSymbol();
        Reporter.ExpectColon();
        var value = Expression();

        return new StructMemberInitialization(member, value);
    });




    private Expression ParseLeftAssociativeBinaryLayoutExpression<T>(Func<Expression> predecessor, params IReadOnlyCollection<TokenType> operators)
        where T : BinaryLayoutExpression, IBinaryLayoutExpressionFactory
        => ParseAssociativeBinaryLayoutExpression<T>(predecessor, false, operators);


    private Expression ParseRightAssociativeBinaryLayoutExpression<T>(Func<Expression> predecessor, params IReadOnlyCollection<TokenType> operators)
        where T : BinaryLayoutExpression, IBinaryLayoutExpressionFactory
        => ParseAssociativeBinaryLayoutExpression<T>(predecessor, true, operators);




    private Expression ParseAssociativeBinaryLayoutExpression<T>(Func<Expression> predecessor, bool rightAssociative = false,
        params IReadOnlyCollection<TokenType> operators) where T : BinaryLayoutExpression, IBinaryLayoutExpressionFactory
    {
        var expression = predecessor();

        while (Match(operators))
        {
            var @operator = Iterator.Previous();
            var right = rightAssociative ? ParseAssociativeBinaryLayoutExpression<T>(predecessor, rightAssociative, operators) : predecessor();
            expression = T.Create(expression, right, @operator.Type, new Span(expression.Location, right.Location));
        }

        return expression;
    }




    private Expression ParseRightAssociativeUnaryLayoutExpression<T>(Func<Expression> predecessor, params IReadOnlyCollection<TokenType> operators)
        where T : UnaryLayoutExpression, IUnaryLayoutExpressionFactory
    {
        if (Match(operators))
        {
            var @operator = Iterator.Previous();
            var expression = ParseRightAssociativeUnaryLayoutExpression<T>(predecessor, operators);

            return T.Create(expression, @operator.Type, new Span(@operator, expression.Location));
        }

        return predecessor();
    }




    private GenericDeclaration ParseGenericDeclaration()
    {
        var type = TryParseTypeSyntax()!;
        var symbol = Reporter.ExpectSymbol();

        return new GenericDeclaration(type, symbol);
    }
}
