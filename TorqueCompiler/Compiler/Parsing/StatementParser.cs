using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.Diagnostics.Catalogs;
using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.Parsing;




public partial class Parser
{
        private Statement? Statement()
    {
        switch (Iterator.Peek().Type)
        {
        case TokenType.KwReturn: return Return();
        case TokenType.LeftCurlyBracket: return Block();
        case TokenType.KwIf: return If();
        case TokenType.KwWhile: return While();
        case TokenType.KwLoop: return Loop();
        case TokenType.KwFor: return For();
        case TokenType.KwBreak: return Break();
        case TokenType.KwContinue: return Continue();
        case TokenType.KwImport: return Import();

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




    public Statement Import()
    {
        var keyword = Iterator.Advance();
        var path = DoWhileToken(TokenType.Dot, () => Reporter.ExpectIdentifier());
        var identifierPath = path.Select(token => new SymbolSyntax(token)).ToArray();
        Reporter.ExpectEndOfStatement();

        var location = new Span(keyword.Location, path.Last().Location);
        return new ImportStatement(identifierPath, location);
    }
}
