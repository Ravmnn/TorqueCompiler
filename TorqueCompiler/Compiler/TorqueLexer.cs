using System.Collections.Generic;
using System.Linq;

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
        var character = Advance();

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
            case '{': return TokenFromTokenType(TokenType.LeftCurlyBracket);
            case '}': return TokenFromTokenType(TokenType.RightCurlyBracket);
            case '[': return TokenFromTokenType(TokenType.LeftSquareBracket);
            case ']': return TokenFromTokenType(TokenType.RightSquareBracket);

            case '\'': return Char();

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
            return Number();

        Report(Diagnostic.LexerCatalog.UnexpectedToken);
        return null;
    }




    private Token Char()
    {
        var quoteLocation = GetCurrentLocation();
        var text = ScanStringText('\'');

        var data = EncodeString(text, quoteLocation);
        ReportCharErrors(data, quoteLocation);

        return TokenFromTokenType(TokenType.CharValue, (ulong)data[0]);
    }


    private void ReportCharErrors(IReadOnlyList<byte> data, SourceLocation quoteLocation)
    {
        if (data.Count == 0)
            Report(Diagnostic.LexerCatalog.SingleCharacterEmpty);

        if (AtEnd())
            Report(Diagnostic.LexerCatalog.UnclosedSingleCharacterString, location: quoteLocation);

        else if (data.Count > 1)
            Report(Diagnostic.LexerCatalog.SingleCharacterMoreThanOne);
    }


    private string ScanStringText(char delimiter)
    {
        AdvanceString(delimiter);

        var text = GetCurrentTokenLexeme();
        text = text.Remove(0, 1);
        text = text.Remove(text.Length - 1, 1);

        return text;
    }


    private void AdvanceString(char delimiter)
    {
        while (!AtEnd())
        {
            if (Peek() == delimiter)
                break;

            if (Peek() == '\\')
                Advance();

            Advance();
        }

        Advance(); // advance the string delimiter
    }


    private IReadOnlyList<byte> EncodeString(string text, SourceLocation quoteLocation)
    {
        var encoder = new StringTokenEncoder(text);
        var data = encoder.ToASCII();
        AddStringEncoderDiagnosticsToThis(encoder, quoteLocation);

        return data;
    }


    private void AddStringEncoderDiagnosticsToThis(StringTokenEncoder encoder, SourceLocation stringQuote)
    {
        var diagnostics = encoder.Diagnostics;

        for (var i = 0; i < diagnostics.Count; i++)
        {
            var diagnostic = diagnostics[i];
            var location = diagnostic.Location!.Value;
            diagnostics[i] = diagnostic with { Location = // "+ 1" here is to jump the quote
                new SourceLocation(stringQuote.Start + location.Start + 1, stringQuote.End + location.End, stringQuote.Line) };
        }

        Diagnostics.AddRange(diagnostics);
    }




    private void Comment() => AdvanceComment();

    private void AdvanceComment()
    {
        while (Peek() != '\n' && !AtEnd())
            Advance();
    }




    private void MultilineComment()
    {
        var startLocation = GetCurrentLocation();
        AdvanceMultilineComment();

        if (AtEnd())
        {
            Report(Diagnostic.LexerCatalog.UnclosedMultilineComment, location: startLocation);
            return;
        }

        Advance(); // advance '<'
        Advance(); // advance '#'
    }


    private void AdvanceMultilineComment()
    {
        while (Peek() != '<' && PeekNext() != '#' && !AtEnd())
            Advance();
    }




    private Token Identifier()
    {
        AdvanceIdentifier();

        var lexeme = GetCurrentTokenLexeme();

        return lexeme switch
        {
            _ when lexeme.IsKeyword() => TokenFromTokenType(Token.Keywords[lexeme]),
            _ when lexeme.IsType() => TokenFromTokenType(TokenType.Type),
            _ when lexeme.IsBoolean() => TokenFromTokenType(TokenType.BoolValue, lexeme.ValueFromBool()),

            _ => TokenFromTokenType(TokenType.Identifier)
        };
    }


    private void AdvanceIdentifier()
    {
        while (Peek() is { } @char && char.IsAsciiLetterOrDigit(@char))
            Advance();
    }




    private Token Number()
    {
        AdvanceWhileDigit();

        var lexeme = GetCurrentTokenLexeme();
        var isFloat = lexeme.Contains('.');

        if (lexeme.Count(character => character == '.') > 1)
            Report(Diagnostic.LexerCatalog.MoreThanOneDotInFloatNumber);

        object? value;

        // don't use ternary operator or any other expression, as they implicitly convert the value to "double"
        if (isFloat)
            value = lexeme.ValueFromFloat();
        else
            value = lexeme.ValueFromInteger();

        return TokenFromTokenType(isFloat ? TokenType.FloatValue : TokenType.IntegerValue, value);
    }


    private void AdvanceWhileDigit()
    {
        while (Peek() is { } character)
        {
            if (!char.IsAsciiDigit(character) && character != '.')
                break;

            if (character == '.' && !IsNextDigit())
                break;

            Advance();
        }
    }


    private bool IsNextDigit()
        => PeekNext() is { } next && char.IsAsciiDigit(next);




    public override Diagnostic Report(Diagnostic.LexerCatalog item, IReadOnlyList<object>? arguments = null, SourceLocation? location = null)
        => base.Report(item, arguments, location ?? GetCurrentLocation());




    private Token TokenFromTokenType(TokenType type, object? value = null)
        => new Token(GetCurrentTokenLexeme(), type, GetCurrentLocation(), value);


    private SourceLocation GetCurrentLocation()
        => new SourceLocation(_startInLine, _endInLine, _line);


    private string GetCurrentTokenLexeme()
        => Source[_start .. _end];




    private char Advance()
    {
        if (AtEnd())
            return Previous();

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


    private char Previous()
        => Source[_end - 1];


    private char Peek()
        => AtEnd() ? Previous() : Source[_end];


    private char PeekNext()
        => AtEnd() ? Peek() : Source[_end + 1];


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
