using System;
using System.Collections.Generic;
using System.Diagnostics;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler;




public class TorqueParser : IIterator<Token>
{
    private readonly List<Statement> _statements = [];
    private readonly List<Modifier> _currentModifiers = [];

    public IIterator<Token> Iterator => this;
    public TorqueParserReporter Reporter { get; }


    public int Current { get; set; }
    public IReadOnlyList<Token> Source { get; }




    public TorqueParser(IReadOnlyList<Token> source)
    {
        Reporter = new TorqueParserReporter(this);

        Source = source;
    }




    public IReadOnlyList<Statement> Parse()
    {
        while (!Iterator.AtEnd())
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
        if (DeclarationWithModifiers() is { } declaration)
            _statements.Add(declaration);
    }


    private void Synchronize()
    {
        Iterator.Advance();

        while (!Iterator.AtEnd())
        {
            switch (Iterator.Peek().Type)
            {
                case TokenType.SemiColon:
                case TokenType.KwAlias:
                case TokenType.KwReturn:
                case TokenType.KwIf:
                case TokenType.KwElse:
                case TokenType.KwWhile:
                case TokenType.KwBreak:
                case TokenType.KwContinue:
                    Iterator.Advance();
                    return;
            }

            Iterator.Advance();
        }
    }




    private void ParseModifiersIfAny()
    {
        while (MatchAnyModifier())
            _currentModifiers.Add(new Modifier(Iterator.Previous()));
    }


    private void ClearCurrentModifiers()
        => _currentModifiers.Clear();




    #region Statements

    private Statement? DeclarationWithModifiers()
    {
        ParseModifiersIfAny();
        var statement = DeclarationOrStatement();
        ClearCurrentModifiers();

        return statement;
    }


    private Statement? DeclarationOrStatement()
    {
        var peek = Iterator.Peek();
        return peek.Type switch
        {
            _ when IsCurrentGenericDeclaration() => VariableOrFunctionDeclaration(),
            TokenType.KwLet => TypeInferredVariableDeclaration(),

            _ => Statement()
        };
    }


    private void AddModifiersToModificableDeclaration(Statement? statement)
    {
        if (statement is IModificable modificable)
            modificable.Modifiers = _currentModifiers.ToArray(); // must be a copy
    }




    private Statement VariableOrFunctionDeclaration()
    {
        var genericDeclaration = ParseGenericDeclaration();

        if (Check(TokenType.LeftParen))
            return FunctionDeclaration(genericDeclaration);

        return VariableDeclaration(genericDeclaration);
    }




    private Statement TypeInferredVariableDeclaration()
    {
        Iterator.Advance();
        var symbol = Reporter.ExpectSymbol();

        var variable = CompleteVariableDeclaration(new GenericDeclaration(null!, symbol));
        variable.InferType = true;

        return variable;
    }




    private Statement VariableDeclaration(GenericDeclaration genericDeclaration)
    {
        Statement? variable;

        if (Match(TokenType.SemiColon))
            variable = DefaultVariableDeclaration(genericDeclaration);
        else
            variable = CompleteVariableDeclaration(genericDeclaration);

        AddModifiersToModificableDeclaration(variable);
        return variable;
    }


    private static SugarDefaultDeclarationStatement DefaultVariableDeclaration(GenericDeclaration genericDeclaration)
        => new SugarDefaultDeclarationStatement(genericDeclaration.Type, genericDeclaration.Name);


    private VariableDeclarationStatement CompleteVariableDeclaration(GenericDeclaration genericDeclaration)
    {
        Reporter.Expect(TokenType.Equal, ParserCatalog.ExpectAssignmentOperator);
        var value = Expression();
        Reporter.ExpectEndOfStatement();

        var location = genericDeclaration.Name.Location;
        return new VariableDeclarationStatement(genericDeclaration.Type, genericDeclaration.Name, value, location);
    }




    private Statement FunctionDeclaration(GenericDeclaration genericDeclaration)
    {
        Reporter.ExpectLeftParen();
        var parameters = FunctionParameters();
        Reporter.ExpectRightParen();

        var function = new FunctionDeclarationStatement(genericDeclaration.Type, genericDeclaration.Name, parameters, null);
        AddModifiersToModificableDeclaration(function);

        if (!Match(TokenType.SemiColon))
            function.Body = (Block() as BlockStatement)!;

        return function;
    }


    private IReadOnlyList<GenericDeclaration> FunctionParameters()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(ParseGenericDeclaration);
    }




    private Statement? Statement()
    {
        switch (Iterator.Peek().Type)
        {
        case TokenType.KwAlias: return Alias();
        case TokenType.KwStruct: return Struct();
        case TokenType.KwReturn: return Return();
        case TokenType.LeftCurlyBracket: return Block();
        case TokenType.KwIf: return If();
        case TokenType.KwWhile: return While();
        case TokenType.KwLoop: return Loop();
        case TokenType.KwFor: return For();
        case TokenType.KwBreak: return Break();
        case TokenType.KwContinue: return Continue();

        // some tokens only makes sense when together with another,
        // but parser exceptions may break that "together", leaving those
        // tokens without any processing. To avoid unnecessary error messages,
        // some tokens should be ignored:

        case TokenType.RightCurlyBracket:
            Iterator.Advance();

            if (Reporter.HasReports) // something already went wrong, ignore
                return null;

            Reporter.ReportAndThrow(ParserCatalog.UnexpectedToken);
            throw new UnreachableException();


        default: return ExpressionStatement();
        }
    }




    private Statement ExpressionStatement()
    {
        var expression = Expression();
        Reporter.ExpectEndOfStatement();

        return new ExpressionStatement(expression);
    }




    private Statement Alias()
    {
        var keyword = Iterator.Advance();
        var name = Reporter.ExpectSymbol();
        Reporter.ExpectAssignment();

        var type = TryParseTypeSyntax()!;
        var end = Reporter.ExpectEndOfStatement();

        var location = new Span(keyword, end);
        return new AliasDeclarationStatement(name, type, location);
    }




    private Statement Struct()
    {
        var keyword = Iterator.Advance();
        var symbol = Reporter.ExpectSymbol();

        Reporter.ExpectLeftCurlyBracket();
        var fields = ParseStructMembers();
        Reporter.ExpectRightCurlyBracket();

        var location = new Span(keyword, symbol.Location);
        return new StructDeclarationStatement(symbol, fields, location);
    }


    private IReadOnlyList<GenericDeclaration> ParseStructMembers()
    {
        var fields = new List<GenericDeclaration>();

        while (!Check(TokenType.RightCurlyBracket))
        {
            fields.Add(ParseGenericDeclaration());
            Reporter.ExpectEndOfStatement();
        }

        return fields;
    }




    private Statement Return()
    {
        var keyword = Iterator.Advance();
        Expression? expression = null;

        if (!Check(TokenType.SemiColon))
            expression = Expression();

        Reporter.ExpectEndOfStatement();

        return new ReturnStatement(keyword, expression);
    }




    private Statement Block()
    {
        var block = new List<Statement>();
        var start = Reporter.Expect(TokenType.LeftCurlyBracket, ParserCatalog.ExpectBlock);

        while (!Iterator.AtEnd() && !Check(TokenType.RightCurlyBracket))
            if (DeclarationWithModifiers() is { } declaration)
                block.Add(declaration);

        Reporter.Expect(TokenType.RightCurlyBracket, ParserCatalog.UnclosedBlock);

        return new BlockStatement(block, start.Location);
    }




    private Statement If()
    {
        var keyword = Iterator.Advance();

        Reporter.ExpectLeftParen();
        var condition = Expression();
        var rightParen = Reporter.ExpectRightParen();

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




    public Statement While()
    {
        var keyword = Iterator.Advance();

        Reporter.ExpectLeftParen();
        var condition = Expression();
        var rightParen = Reporter.ExpectRightParen();

        var body = Statement()!;

        var location = new Span(keyword, rightParen);
        return new WhileStatement(condition, body, null, location);
    }




    public Statement Loop()
    {
        var keyword = Iterator.Advance();
        var body = Statement()!;

        var location = new Span(keyword, keyword);
        return new SugarLoopStatement(body, location);
    }




    public Statement For()
    {
        var keyword = Iterator.Advance();

        Reporter.ExpectLeftParen();

        var initialization = IsCurrentGenericDeclaration() ? VariableDeclaration(ParseGenericDeclaration()) : ExpressionStatement();
        var condition = Expression();
        Reporter.ExpectEndOfStatement();
        var step = Expression();

        Reporter.ExpectRightParen();

        var loop = Statement()!;

        var location = new Span(keyword, Iterator.Previous());
        return new SugarForStatement(initialization, condition, step, loop, location);
    }




    public Statement Break()
    {
        var keyword = Iterator.Advance();
        Reporter.ExpectEndOfStatement();

        return new BreakStatement(keyword);
    }


    public Statement Continue()
    {
        var keyword = Iterator.Advance();
        Reporter.ExpectEndOfStatement();

        return new ContinueStatement(keyword);
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
            var type = TryParseTypeSyntax()!;
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
            var rightSquareBracket = Reporter.ExpectRightSquareBracket();
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
            var parenRight = Reporter.ExpectRightParen();
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
            var @operator = Iterator.Previous();
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

    #endregion




    #region Type Syntax

    private TypeSyntax? TryParseTypeSyntax()
        => TryParseTypeSyntax(new Dictionary<TokenType, Func<TypeSyntax, TypeSyntax?>>
        {
            { TokenType.Star, ParsePointerTypeName },
            { TokenType.LeftSquareBracket, ParseArrayTypeName },
            { TokenType.Colon, ParseFunctionTypeName }
        });


    private TypeSyntax? TryParseTypeSyntax(Dictionary<TokenType, Func<TypeSyntax, TypeSyntax?>> processors)
    {
        var typeNameSymbol = Reporter.ExpectSymbolOrPrimitiveType();
        TypeSyntax type = new BaseTypeSyntax(typeNameSymbol);

        while (true)
        {
            var result = ModifyCurrentTypeNameFromProcessors(ref type, processors);

            if (result is null)
                return null;

            if (!result.Value)
                break;
        }

        return type;
    }


    private bool? ModifyCurrentTypeNameFromProcessors(ref TypeSyntax type, Dictionary<TokenType, Func<TypeSyntax, TypeSyntax?>> processors)
    {
        foreach (var (token, processor) in processors)
        {
            if (!Match(token))
                continue;

            if (processor(type) is not { } validType)
                return null;

            type = validType;
            return true;
        }

        return false;
    }


    private TypeSyntax ParsePointerTypeName(TypeSyntax type)
        => new PointerTypeSyntax(type);


    private TypeSyntax? ParseArrayTypeName(TypeSyntax type)
    {
        if (!Check(TokenType.RightSquareBracket))
            return null;

        return new PointerTypeSyntax(type);
    }


    private TypeSyntax ParseFunctionTypeName(TypeSyntax type)
    {
        Reporter.ExpectLeftParen();
        var parameters = ParseFunctionTypeNameParameters();
        Reporter.ExpectRightParen();

        return new FunctionTypeSyntax(type, parameters);
    }


    private IReadOnlyList<TypeSyntax> ParseFunctionTypeNameParameters()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(TryParseTypeSyntax)!;
    }

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




    private bool IsCurrentGenericDeclaration()
    {
        var current = Current;

        var currentTypeIsValid = IsCurrentTypeValid();
        var name = Iterator.Peek();

        Current = current;

        return IsValidIdentifier(name) && currentTypeIsValid;
    }

    private bool IsCurrentTypeValid()
    {
        var type = Iterator.Peek();

        if (IsValidIdentifierOrPrimitiveType(type))
            return TryParseTypeSyntax() is not null;

        return false;
    }


    private bool IsValidIdentifierOrPrimitiveType(Token identifier)
    {
        var isPrimitiveType = identifier.Lexeme.IsType();
        return IsValidIdentifier(identifier) || isPrimitiveType;
    }


    private bool IsValidIdentifier(Token identifier)
        => identifier.Type == TokenType.Identifier && !identifier.Lexeme.IsReserved();




    private bool MatchAnyModifier()
    {
        if (!Iterator.Peek().Lexeme.IsModifier())
            return false;

        Iterator.Advance();
        return true;
    }


    public bool Match(params IReadOnlyList<TokenType> tokens)
    {
        foreach (var token in tokens)
        {
            if (!Check(token))
                continue;

            Iterator.Advance();
            return true;
        }

        return false;
    }


    public bool Check(TokenType token)
    {
        if (Iterator.AtEnd())
            return false;

        return Iterator.Peek().Type == token;
    }

    #endregion
}
