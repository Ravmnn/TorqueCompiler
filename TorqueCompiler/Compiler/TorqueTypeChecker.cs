using System;
using System.Collections.Generic;
using System.Diagnostics;
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




    // BUG: casting void to any type results in a loop
    private void ReportIfDiffers(Type expected, Type got, TokenLocation location)
    {
        if (expected == got)
            return;

        if (got.IsVoid)
            Report(Diagnostic.TypeCheckerCatalog.ExpressionDoesNotReturnAnyValue, [], location);
        else
            Report(Diagnostic.TypeCheckerCatalog.TypeDiffers, [expected.ToString(), got.ToString()], location);
    }


    private void ReportIfNotAPointer(Type type, TokenLocation location)
    {
        if (type.IsPointer)
            return;

        Report(Diagnostic.TypeCheckerCatalog.PointerExpected, location: location);
    }


    private void ReportIfVoid(Type type, TokenLocation location)
    {
        if (!type.IsVoid)
            return;

        Report(Diagnostic.TypeCheckerCatalog.CannotUseVoidHere, location: location);
    }




    private Type TypeFromTypeName(TypeName typeName, bool reportIfVoid = true)
    {
        var type = typeName switch
        {
            FunctionTypeName function => FunctionTypeFromTypeName(function),
            _ => RawTypeFromTypeName(typeName)
        };

        if (reportIfVoid)
            ReportIfVoid(type, typeName.BaseType);

        return type;
    }


    private Type RawTypeFromTypeName(TypeName typeName)
        => new Type(typeName.BaseType.TokenToPrimitive(), typeName.IsPointer);


    private FunctionType FunctionTypeFromTypeName(FunctionTypeName typeName)
    {
        var parameters = (from parameter in typeName.ParametersType select TypeFromTypeName(parameter)).ToArray();
        return new FunctionType(typeName.ReturnType.TokenToPrimitive(), parameters);
    }




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

        var returnType = TypeFromTypeName(functionSyntax.ReturnType, false);
        var parameterTypes = (from parameter in functionSyntax.Parameters.ToArray()
            select TypeFromTypeName(parameter.Type)).ToArray();

        statement.Symbol.Type = new FunctionType(returnType, parameterTypes);

        for (var i = 0; i < parameterTypes.Length; i++)
            statement.Symbol.Parameters[i].Type = parameterTypes[i];


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
        expression.Value = expression.Type.BaseType switch
        {
            PrimitiveType.Bool => token.ValueFromBool(),
            PrimitiveType.Char => throw new NotImplementedException(),

            _ => token.ValueFromNumber()
        };

        return expression.Type!;
    }




    public Type ProcessBinary(BoundBinaryExpression expression)
    {
        var leftType = Process(expression.Left);
        var rightType = Process(expression.Right);

        ReportIfDiffers(leftType, rightType, expression.Right.Source());

        return expression.Type!;
    }




    public Type ProcessUnary(BoundUnaryExpression expression)
    {
        var unarySyntax = (expression.Syntax as UnaryExpression)!;

        var type = Process(expression.Expression);

        switch (unarySyntax.Operator.Type)
        {
            case TokenType.Star: ReportIfNotAPointer(type, expression.Source()); break;
            case TokenType.Minus: break;
            case TokenType.Exclamation: ReportIfDiffers(PrimitiveType.Bool, type, expression.Source()); break;

            default: throw new UnreachableException();
        }

        return expression.Type!;
    }




    public Type ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public Type ProcessSymbol(BoundSymbolExpression expression)
    {
        return expression.Type!;
    }




    public Type ProcessAssignment(BoundAssignmentExpression expression)
    {
        var referenceType = Process(expression.Reference);
        var valueType = Process(expression.Value);

        ReportIfDiffers(referenceType, valueType, expression.Value.Source());

        return expression.Type!;
    }


    public Type ProcessAssignmentReference(BoundAssignmentReferenceExpression expression)
        => Process(expression.Reference);




    public Type ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        Process(expression.Pointer);
        return expression.Type;
    }




    public Type ProcessCall(BoundCallExpression expression)
    {
        Process(expression.Callee);

        // TODO: add function types for variables (delegates). After that, use that type to check if the arguments match the function's parameter types

        foreach (var argument in expression.Arguments)
            Process(argument);

        return expression.Type;
    }




    public Type ProcessCast(BoundCastExpression expression)
    {
        var castSyntax = (expression.Syntax as CastExpression)!;

        Process(expression.Value);
        expression.Type = TypeFromTypeName(castSyntax.Type);

        return expression.Type!;
    }
}
