using System;
using System.Collections.Generic;

using Torque.Compiler.Tokens;
using Torque.Compiler.AST.Statements;


namespace Torque.Compiler;




public partial class TorqueParser : IIterator<Token>
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
}
