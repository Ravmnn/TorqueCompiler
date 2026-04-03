using System.Collections.Generic;

using Torque.Compiler.AST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;
using Torque.Compiler.Tokens;


namespace Torque.Compiler;




public partial class TorqueParser
{
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
            TokenType.KwAlias => Alias(),
            TokenType.KwStruct => Struct(),

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
            function.Body = (Block() as BlockStatement);

        return function;
    }


    private IReadOnlyList<GenericDeclaration> FunctionParameters()
    {
        if (Check(TokenType.RightParen))
            return [];

        return DoWhileComma(ParseGenericDeclaration);
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
}
