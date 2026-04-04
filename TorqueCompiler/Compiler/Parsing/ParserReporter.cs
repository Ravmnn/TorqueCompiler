using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;
using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.Parsing;




public sealed class ParserReporter(Parser parser) : DiagnosticReporter<ParserCatalog>(parser.SourceCode)
{
    public Parser Parser { get; } = parser;




    [DoesNotReturn]
    public override void ReportAndThrow(ParserCatalog item, IReadOnlyList<object>? arguments = null, Span? location = null)
        => base.ReportAndThrow(item, arguments, location ?? Parser.Iterator.Peek().Location);




    public bool ReportIfIdentifierIsReserved(Token token)
    {
        if (!token.Lexeme.IsReserved())
            return false;

        ReportAndThrow(ParserCatalog.ReservedIdentifier, location: token);
        return true;
    }


    public Token Expect(TokenType token, ParserCatalog item, Span? location = null)
    {
        if (Parser.Check(token))
            return Parser.Iterator.Advance();

        ReportAndThrow(item, location: location);
        throw new UnreachableException();
    }


    public Token ExpectEndOfStatement()
        => Expect(TokenType.SemiColon, ParserCatalog.ExpectSemicolonAfterStatement);


    public Token ExpectAssignment()
        => Expect(TokenType.Equal, ParserCatalog.ExpectAssignment);


    public Token ExpectIdentifier(bool primitiveTypeAllowed = false)
    {
        var identifier = Expect(TokenType.Identifier, ParserCatalog.ExpectIdentifier);

        if (!primitiveTypeAllowed || !identifier.Lexeme.IsType())
            ReportIfIdentifierIsReserved(identifier);

        return identifier;
    }


    public SymbolSyntax ExpectSymbol()
        => new SymbolSyntax(ExpectIdentifier());

    public SymbolSyntax ExpectSymbolOrPrimitiveType()
        => new SymbolSyntax(ExpectIdentifier(true));


    public Token ExpectLeftParen()
        => Expect(TokenType.LeftParen, ParserCatalog.ExpectLeftParen);

    public Token ExpectRightParen()
        => Expect(TokenType.RightParen, ParserCatalog.ExpectRightParen);


    public Token ExpectLeftSquareBracket()
        => Expect(TokenType.LeftSquareBracket, ParserCatalog.ExpectLeftSquareBracket);

    public Token ExpectRightSquareBracket()
        => Expect(TokenType.RightSquareBracket, ParserCatalog.ExpectRightSquareBracket);


    public Token ExpectLeftCurlyBracket()
        => Expect(TokenType.LeftCurlyBracket, ParserCatalog.ExpectLeftCurlyBracket);

    public Token ExpectRightCurlyBracket()
        => Expect(TokenType.RightCurlyBracket, ParserCatalog.ExpectRightCurlyBracket);


    public Token ExpectLiteralInteger()
        => Expect(TokenType.IntegerValue, ParserCatalog.ExpectLiteralInteger);


    public Token ExpectColon()
        => Expect(TokenType.Colon, ParserCatalog.ExpectColon);
}
