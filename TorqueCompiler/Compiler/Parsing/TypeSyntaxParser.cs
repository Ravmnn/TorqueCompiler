using System;
using System.Collections.Generic;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.Compiler.Parsing;




public partial class Parser
{
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
}
