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


    public string Source { get; set; } = source;




    public override Diagnostic Report(Diagnostic.LexerCatalog item, object[]? arguments = null, TokenLocation? location = null)
        => base.Report(item, arguments, location ?? GetCurrentLocation());




    public IEnumerable<Token> Tokenize()
    {
        var tokens = new List<Token>();

        Reset();

        while (!AtEnd())
        {
            _start = _end;
            _startInLine = _endInLine;

            if (TokenizeNext() is { } token)
                tokens.Add(token);
        }

        return tokens;
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

            case ':': return TokenFromType(TokenType.Colon);
            case ';': return TokenFromType(TokenType.SemiColon);
            case ',': return TokenFromType(TokenType.Comma);
            case '-': return Match('>') ? TokenFromType(TokenType.Arrow) : TokenFromType(TokenType.Minus);
            case '+': return TokenFromType(TokenType.Plus);
            case '*': return TokenFromType(TokenType.Star);
            case '/': return TokenFromType(TokenType.Slash);
            case '=': return TokenFromType(TokenType.Equal);
            case '&': return TokenFromType(TokenType.Ampersand);
            case '(': return TokenFromType(TokenType.ParenLeft);
            case ')': return TokenFromType(TokenType.ParenRight);
            case '{': return TokenFromType(TokenType.CurlyBraceLeft);
            case '}': return TokenFromType(TokenType.CurlyBraceRight);

            case '#':
                if (Match('>'))
                    MultilineComment();
                else
                    Comment();

                return null;
        }

        if (char.IsAsciiLetter(character))
            return Identifier();

        if (char.IsAsciiDigit(character))
            return Value();

        Report(Diagnostic.LexerCatalog.UnexpectedToken);
        return null;
    }


    private Token TokenFromType(TokenType type)
        => new Token(GetCurrentTokenLexeme(), type, GetCurrentLocation());


    private TokenLocation GetCurrentLocation()
        => new TokenLocation(_startInLine, _endInLine, _line);


    private string GetCurrentTokenLexeme()
        => Source[_start .. _end];




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

        var currentLexeme = GetCurrentTokenLexeme();

        if (currentLexeme.IsKeyword())
            return TokenFromType(Token.Keywords[currentLexeme]);

        if (currentLexeme.IsType())
            return TokenFromType(TokenType.Type);

        if (currentLexeme.IsBoolean())
            return TokenFromType(TokenType.Value);

        return TokenFromType(TokenType.Identifier);
    }


    private Token Value()
    {
        while (Peek() is { } @char && char.IsAsciiDigit(@char))
            Advance();

        return TokenFromType(TokenType.Value);
    }




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
