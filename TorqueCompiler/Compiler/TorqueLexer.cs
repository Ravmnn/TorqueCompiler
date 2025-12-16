using System;
using System.Collections.Generic;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueLexer(string source) : DiagnosticReporter<Diagnostic.LexerCatalog>
{
    private int _startInLine;
    private int _endInLine;
    private int _line;

    private int _start;
    private int _end;


    public string Source { get; } = source;




    public IReadOnlyList<Token> Tokenize()
    {
        var tokens = new List<Token>();
        Reset();

        while (!AtEnd())
        {
            NextTokenStart();

            if (TokenizeNext() is { } token)
                tokens.Add(token);
        }

        return tokens;
    }


    private void NextTokenStart()
    {
        _start = _end;
        _startInLine = _endInLine;
    }


    private void Reset()
    {
        _start = _end = 0;
        _startInLine = _endInLine = 0;
        _line = 1;

        Diagnostics.Clear();
    }




    private Token? TokenizeNext()
    {
        if (Advance() is not { } character)
            throw new NullReferenceException("Current character is null.");

        switch (character)
        {
            case ' ':
            case '\t':
            case '\n':
            case '\r':
                return null;

            case ':': return TokenFromTokenType(TokenType.Colon);
            case ';': return TokenFromTokenType(TokenType.SemiColon);
            case ',': return TokenFromTokenType(TokenType.Comma);
            case '-': return Match('>') ? TokenFromTokenType(TokenType.Arrow) : TokenFromTokenType(TokenType.Minus);
            case '+': return TokenFromTokenType(TokenType.Plus);
            case '*': return TokenFromTokenType(TokenType.Star);
            case '/': return TokenFromTokenType(TokenType.Slash);
            case '>': return Match('=') ? TokenFromTokenType(TokenType.GreaterThanOrEqual) : TokenFromTokenType(TokenType.GreaterThan);
            case '<': return Match('=') ? TokenFromTokenType(TokenType.LessThanOrEqual) : TokenFromTokenType(TokenType.LessThan);
            case '=': return Match('=') ? TokenFromTokenType(TokenType.Equality) : TokenFromTokenType(TokenType.Equal);
            case '!': return Match('=') ? TokenFromTokenType(TokenType.Inequality) :TokenFromTokenType(TokenType.Exclamation);
            case '&': return Match('&') ? TokenFromTokenType(TokenType.LogicAnd) : TokenFromTokenType(TokenType.Ampersand);
            case '|': return Match('|') ? TokenFromTokenType(TokenType.LogicOr) : TokenFromTokenType(TokenType.Pipe);
            case '(': return TokenFromTokenType(TokenType.LeftParen);
            case ')': return TokenFromTokenType(TokenType.RightParen);
            case '{': return TokenFromTokenType(TokenType.LeftCurlyBrace);
            case '}': return TokenFromTokenType(TokenType.RightCurlyBrace);

            case '#':
                if (Match('>'))
                    MultilineComment();
                else
                    Comment();

                return null;
        }

        return TokenizeLiteralOrReport(character);
    }


    private Token? TokenizeLiteralOrReport(char character)
    {
        if (char.IsAsciiLetter(character))
            return Identifier();

        if (char.IsAsciiDigit(character))
            return Value();

        Report(Diagnostic.LexerCatalog.UnexpectedToken);
        return null;
    }




    private void Comment()
    {
        while (Peek() != '\n' && !AtEnd())
            Advance();
    }


    private void MultilineComment()
    {
        var startLocation = GetCurrentLocation();

        while (Peek() != '<' && PeekNext() != '#' && !AtEnd())
            Advance();

        if (AtEnd())
        {
            Report(Diagnostic.LexerCatalog.UnclosedMultilineComment, location: startLocation);
            return;
        }

        Advance(); // advance '<'
        Advance(); // advance '#'
    }




    private Token Identifier()
    {
        while (Peek() is { } @char && char.IsAsciiLetterOrDigit(@char))
            Advance();

        var lexeme = GetCurrentTokenLexeme();

        return lexeme switch
        {
            _ when lexeme.IsKeyword() => TokenFromTokenType(Token.Keywords[lexeme]),
            _ when lexeme.IsType() => TokenFromTokenType(TokenType.Type),
            _ when lexeme.IsBoolean() => TokenFromTokenType(TokenType.Value),

            _ => TokenFromTokenType(TokenType.Identifier)
        };
    }


    private Token Value()
    {
        while (Peek() is { } @char && char.IsAsciiDigit(@char))
            Advance();

        return TokenFromTokenType(TokenType.Value);
    }




    public override Diagnostic Report(Diagnostic.LexerCatalog item, IReadOnlyList<object>? arguments = null, SourceLocation? location = null)
        => base.Report(item, arguments, location ?? GetCurrentLocation());




    private Token TokenFromTokenType(TokenType type)
        => new Token(GetCurrentTokenLexeme(), type, GetCurrentLocation());


    private SourceLocation GetCurrentLocation()
        => new SourceLocation(_startInLine, _endInLine, _line);


    private string GetCurrentTokenLexeme()
        => Source[_start .. _end];




    private char? Advance()
    {
        if (AtEnd())
            return null;

        _endInLine++;

        if (Peek() == '\n')
            NextLine();

        return Source[_end++];
    }

    private void NextLine()
    {
        _startInLine = _endInLine = 0;
        _line++;
    }


    private char? Peek()
        => AtEnd() ? null : Source[_end];


    private char? PeekNext()
        => AtEnd() ? null : Source[_end];


    private bool Match(char character)
    {
        if (Peek() != character)
            return false;

        Advance();
        return true;
    }


    private bool AtEnd()
        => _end >= Source.Length;
}
