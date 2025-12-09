using System;
using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueTypeChecker(IEnumerable<BoundStatement> statements)
    : DiagnosticReporter<Diagnostic.TypeCheckerCatalog>, IBoundStatementProcessor, IBoundExpressionProcessor<Type>
{
    public const PrimitiveType DefaultLiteralType = PrimitiveType.Int32;




    private PrimitiveType? _expectedReturnType;


    public IEnumerable<BoundStatement> Statements { get; } = statements;


    // TODO: add implicit casts

    public void Check()
    {
        Diagnostics.Clear();

        foreach (var statement in Statements)
            Process(statement);
    }




    private void ReportIfDiffers(Type expected, Type got, TokenLocation location)
    {
        if (expected == got)
            return;

        Report(Diagnostic.TypeCheckerCatalog.TypeDiffers, [expected.ToString(), got.ToString()], location);
    }




    private static Type TypeFromTypeName(TypeName typeName)
        => new Type(typeName.BaseType.TokenToPrimitive(), typeName.IsPointer);




    public void Process(BoundStatement statement)
        => statement.Process(this);




    public void ProcessExpression(BoundExpressionStatement statement)
        => Process(statement.Expression);




    public void ProcessDeclaration(BoundDeclarationStatement statement)
    {
        var declarationSyntax = (statement.Syntax as DeclarationStatement)!;

        var symbolType = TypeFromTypeName(declarationSyntax.Type);
        var valueType = Process(statement.Value);

        statement.Symbol.Type = symbolType;
        ReportIfDiffers(symbolType, valueType, statement.Value.Source());
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
    {
        var functionSyntax = (statement.Syntax as FunctionDeclarationStatement)!;

        var returnType = TypeFromTypeName(functionSyntax.ReturnType);
        var parameterTypes = functionSyntax.Parameters.Select(param => TypeFromTypeName(param.Type)).ToArray();

        statement.Symbol.ReturnType = returnType;
        statement.Symbol.Parameters = parameterTypes;


        _expectedReturnType = returnType;

        Process(statement.Body);

        _expectedReturnType = null;
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        // TODO: when void returns is supported, change this
        var value = Process(statement.Expression!);

        ReportIfDiffers(_expectedReturnType!.Value, value, statement.Expression!.Source());
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }








    public Type Process(BoundExpression expression)
        => expression.Process(this);




    public Type ProcessLiteral(BoundLiteralExpression expression)
    {
        var token = expression.Source();

        expression.Type = token.IsBoolean() ? PrimitiveType.Bool : DefaultLiteralType; // TODO: add char notation 'char' (converts to number)
        expression.Value = expression.Type.Value.BaseType switch
        {
            PrimitiveType.Bool => token.ValueFromBool(),
            PrimitiveType.Char => throw new NotImplementedException(),

            _ => token.ValueFromNumber()
        };

        return expression.Type!.Value;
    }




    public Type ProcessBinary(BoundBinaryExpression expression)
    {
        var leftType = Process(expression.Left);
        var rightType = Process(expression.Right);

        ReportIfDiffers(leftType, rightType, expression.Right.Source());

        return expression.Type!.Value;
    }




    public Type ProcessGrouping(BoundGroupingExpression expression)
    {
        Process(expression.Expression);
        return expression.Type!.Value;
    }




    public Type ProcessSymbol(BoundSymbolExpression expression)
    {
        return expression.Type!.Value;
    }




    public Type ProcessAssignment(BoundAssignmentExpression expression)
    {
        var identifierType = Process(expression.Symbol);
        var valueType = Process(expression.Value);

        ReportIfDiffers(identifierType, valueType, expression.Value.Source());

        return expression.Type!.Value;
    }




    public Type ProcessCall(BoundCallExpression expression)
    {
        // TODO: add pointer types, then use them to call functions

        return expression.Type!.Value;
    }




    public Type ProcessCast(BoundCastExpression expression)
    {
        var castSyntax = (expression.Syntax as CastExpression)!;

        Process(expression.Value);
        expression.Type = TypeFromTypeName(castSyntax.Type);

        return expression.Type!.Value;
    }
}
