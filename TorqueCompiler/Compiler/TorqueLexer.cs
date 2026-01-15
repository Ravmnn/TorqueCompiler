using System;
using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler;




public class TorqueLexer(string source) : DiagnosticReporter<LexerCatalog>
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

            case '\'': return ScanChar();
            case '\"': return ScanString();

            case '#':
                if (Match('>'))
                    ScanMultilineComment();
                else
                    ScanComment();

                return null;
        }

        return TokenizeLiteralOrReport(character);
    }


    private Token? TokenizeLiteralOrReport(char character)
    {
        if (char.IsAsciiLetter(character))
            return ScanIdentifier();

        if (char.IsAsciiDigit(character))
            return ScanNumber();

        Report(LexerCatalog.UnexpectedToken);
        return null;
    }




    private Token ScanChar()
    {
        var quoteLocation = GetCurrentLocation();
        var text = ScanStringText('\'');

        var data = EncodeStringToASCII(text, quoteLocation);
        ReportCharErrors(data, quoteLocation);

        return TokenFromTokenType(TokenType.CharValue, data[0]);
    }


    private void ReportCharErrors(IReadOnlyList<byte> data, Span quoteLocation)
    {
        if (data.Count == 0)
            Report(LexerCatalog.SingleCharacterEmpty);

        if (AtEnd())
            Report(LexerCatalog.UnclosedSingleCharacterString, location: quoteLocation);

        else if (data.Count > 1)
            Report(LexerCatalog.SingleCharacterMoreThanOne);
    }




    private Token ScanString()
    {
        var quoteLocation = GetCurrentLocation();
        var text = ScanStringText('\"');

        var data = EncodeStringToASCII(text, quoteLocation);
        ReportStringErrors(quoteLocation);

        return TokenFromTokenType(TokenType.StringValue, data);
    }


    private void ReportStringErrors(Span quoteLocation)
    {
        if (AtEnd())
            Report(LexerCatalog.UnclosedString, location: quoteLocation);
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


    private IReadOnlyList<byte> EncodeStringToASCII(string text, Span quoteLocation)
    {
        var encoder = new StringTokenEncoder(text);
        var data = encoder.ToASCII();
        AddStringEncoderDiagnosticsToThis(encoder, quoteLocation);

        return data;
    }


    private void AddStringEncoderDiagnosticsToThis(StringTokenEncoder encoder, Span stringStartQuoteLocation)
    {
        var diagnostics = encoder.Diagnostics;

        for (var i = 0; i < diagnostics.Count; i++)
            diagnostics[i] = ConvertStringEncoderDiagnosticsToThis(diagnostics[i], stringStartQuoteLocation);

        Diagnostics.AddRange(diagnostics);
    }


    private static Diagnostic ConvertStringEncoderDiagnosticsToThis(Diagnostic diagnostic, Span stringStartQuoteLocation)
    {
        var location = diagnostic.Location!.Value;

        return diagnostic with
        {
            Location = new Span(
                stringStartQuoteLocation.Start + location.Start + 1,
                stringStartQuoteLocation.End + location.End,
                stringStartQuoteLocation.Line
            )
        };
    }




    private void ScanComment() => AdvanceComment();

    private void AdvanceComment()
    {
        while (Peek() != '\n' && !AtEnd())
            Advance();
    }




    private void ScanMultilineComment()
    {
        var startLocation = GetCurrentLocation();
        AdvanceMultilineComment();

        if (AtEnd())
        {
            Report(LexerCatalog.UnclosedMultilineComment, location: startLocation);
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




    private Token ScanIdentifier()
    {
        AdvanceIdentifier();

        var lexeme = GetCurrentTokenLexeme();

        return lexeme switch
        {
            _ when lexeme.IsKeyword() => TokenFromTokenType(Keywords.General[lexeme]),
            _ when lexeme.IsModifier() => TokenFromTokenType(Keywords.Modifiers[lexeme]),
            _ when lexeme.IsType() => TokenFromTokenType(TokenType.Type),
            _ when lexeme.IsLiteralBoolean() => TokenFromTokenType(TokenType.BoolValue, lexeme.ValueFromBool()),

            _ => TokenFromTokenType(TokenType.Identifier)
        };
    }


    private void AdvanceIdentifier()
    {
        while (Peek() is var @char && char.IsAsciiLetterOrDigit(@char))
            Advance();
    }




    private Token ScanNumber()
    {
        AdvanceWhileDigit();
        return TokenFromNumber(GetCurrentTokenLexeme());
    }


    private Token TokenFromNumber(string lexeme)
    {
        if (lexeme.Count(character => character == '.') > 1)
            Report(LexerCatalog.MoreThanOneDotInFloatNumber);

        var value = GetValueFromNumber(lexeme);
        var tokenType = value is double ? TokenType.FloatValue : TokenType.IntegerValue;

        return TokenFromTokenType(tokenType, value);
    }


    private static object GetValueFromNumber(string lexeme)
    {
        object? value;
        var isFloat = lexeme.Contains('.');

        // don't use ternary operator or any other expression, as they implicitly convert the value to "double"
        if (isFloat)
            value = lexeme.ValueFromFloat();
        else
            value = lexeme.ValueFromInteger();

        return value;
    }


    private void AdvanceWhileDigit()
    {
        while (Peek() is var character)
        {
            if (!char.IsAsciiDigit(character) && character != '.')
                break;

            if (character == '.' && !IsNextDigit())
                break;

            Advance();
        }
    }


    private bool IsNextDigit()
        => PeekNext() is var next && char.IsAsciiDigit(next);




    public override Diagnostic Report(LexerCatalog item, IReadOnlyList<object>? arguments = null, Span? location = null)
        => base.Report(item, arguments, location ?? GetCurrentLocation());




    private Token TokenFromTokenType(TokenType type, object? value = null)
        => new Token(GetCurrentTokenLexeme(), type, GetCurrentLocation(), value);


    private Span GetCurrentLocation()
        => new Span(_startInLine, _endInLine, _line);


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
