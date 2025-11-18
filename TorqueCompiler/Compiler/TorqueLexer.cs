using System;
using System.Collections.Generic;


namespace Torque.Compiler;




public class TorqueLexer(string source)
{
    private uint _startInLine;
    private uint _endInLine;
    private uint _line;

    private uint _start;
    private uint _end;


    public string Source { get; set; } = source;




    public IEnumerable<Token> Tokenize()
    {
        var tokens = new List<Token>();

        Reset();

        while (!AtEnd())
        {
            try
            {
                _start = _end;
                _startInLine = _endInLine;

                if (TokenizeNext() is { } token)
                    tokens.Add(token);
            }
            catch (LanguageException exception)
            {
                Torque.LogError(exception);
            }
        }

        return tokens;
    }


    private void Reset()
    {
        _start = _end = 0;
        _startInLine = _endInLine = 0;
        _line = 1;
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
            case '(': return TokenFromType(TokenType.ParenLeft);
            case ')': return TokenFromType(TokenType.ParenRight);

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

        throw TorqueErrors.InvalidToken(GetCurrentLocation());
    }


    private Token TokenFromType(TokenType type)
        => new Token(GetCurrentTokenLexeme(), type, GetCurrentLocation());


    private TokenLocation GetCurrentLocation()
        => new TokenLocation(_startInLine, _endInLine, _line);


    private string GetCurrentTokenLexeme()
        => Source[(int)_start .. (int)_end];




    private void Comment()
    {
        while (Peek() != '\n' && !AtEnd())
            Advance();
    }


    private void MultilineComment()
    {
        var startLocation = GetCurrentLocation();

        while ((Peek() != '<' || PeekNext() != '#') && !AtEnd())
            Advance();

        if (AtEnd())
            throw TorqueErrors.UnclosedMultilineComment(startLocation);

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

        return Source[(int)_end++];
    }

    private void NextLine()
    {
        _startInLine = _endInLine = 0;
        _line++;
    }


    private char? Peek()
        => AtEnd() ? null : Source[(int)_end];


    private char? PeekNext()
        => AtEnd() ? null : Source[(int)_end];


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
