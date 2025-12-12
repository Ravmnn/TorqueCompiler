using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class TorqueTypeChecker(IReadOnlyList<BoundStatement> statements)
    : DiagnosticReporter<Diagnostic.TypeCheckerCatalog>, IBoundStatementProcessor, IBoundExpressionProcessor<Type>
{
    public const PrimitiveType DefaultLiteralType = PrimitiveType.Int32;




    private Type? _expectedReturnType;


    public IReadOnlyList<BoundStatement> Statements { get; } = statements;


    // TODO: add implicit casts

    public void Check()
    {
        Diagnostics.Clear();

        foreach (var statement in Statements)
            Process(statement);
    }








    #region Statements

    public void Process(BoundStatement statement)
        => statement.Process(this);




    public void ProcessExpression(BoundExpressionStatement statement)
        => Process(statement.Expression);




    public void ProcessDeclaration(BoundDeclarationStatement statement)
    {
        var symbolType = TypeFromNonVoidTypeName(statement.Syntax.Type);
        var valueType = Process(statement.Value);

        statement.Symbol.Type = symbolType;
        ReportIfDiffers(symbolType, valueType, statement.Value.Source());
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
    {
        var returnType = TypeFromTypeName(statement.Syntax.ReturnType);
        var parametersType = ParametersTypeFromParametersDeclaration(statement.Syntax.Parameters);

        statement.Symbol.Type = new FunctionType(returnType, parametersType);
        SetFunctionSymbolParametersType(statement.Symbol, parametersType);

        _expectedReturnType = returnType;
        Process(statement.Body);
        _expectedReturnType = null;
    }


    private IReadOnlyList<Type> ParametersTypeFromParametersDeclaration(IReadOnlyList<FunctionParameterDeclaration> parameters)
        => (from parameter in parameters select TypeFromNonVoidTypeName(parameter.Type)).ToArray();


    private void SetFunctionSymbolParametersType(FunctionSymbol symbol, IReadOnlyList<Type> parametersType)
    {
        for (var i = 0; i < parametersType.Count; i++)
            symbol.Parameters[i].Type = parametersType[i];
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        // TODO: check whether the function expects a return but none occurs:
        // we have to check the control flow to correctly detect return issues.
        // create Control-Flow Analysis (CFA) and Control-Flow Graph (CFG)

        // TODO: make that only function declaration can exist in file scope

        ReportIfVoidFunctionReturnsValue(statement);

        if (statement.Expression is null)
            return;

        var value = Process(statement.Expression);

        if (!_expectedReturnType!.IsVoid)
            ReportIfDiffers(_expectedReturnType, value, statement.Expression.Source());
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }

    #endregion








    #region Expression

    public Type Process(BoundExpression expression)
        => expression.Process(this);




    public Type ProcessLiteral(BoundLiteralExpression expression)
    {
        var token = expression.Source();

        expression.Type = TypeOfLiteralToken(token); // TODO: add char notation 'char' (converts to number)
        expression.Value = expression.Type.BaseType switch
        {
            PrimitiveType.Bool => token.ValueFromBool(),
            PrimitiveType.Char => throw new NotImplementedException(),

            _ => token.ValueFromNumber()
        };

        return expression.Type!;
    }


    private PrimitiveType TypeOfLiteralToken(Token literal) => literal switch
    {
        _ when literal.IsBoolean() => PrimitiveType.Bool,
        _ => DefaultLiteralType
    };




    public Type ProcessBinary(BoundBinaryExpression expression)
    {
        var leftType = Process(expression.Left);
        var rightType = Process(expression.Right);

        ReportIfDiffers(leftType, rightType, expression.Right.Source());

        return expression.Type!;
    }




    public Type ProcessUnary(BoundUnaryExpression expression)
    {
        var type = Process(expression.Expression);

        switch (expression.Syntax.Operator.Type)
        {
            case TokenType.Minus: break;
            case TokenType.Exclamation: ReportIfDiffers(PrimitiveType.Bool, type, expression.Source()); break;

            default: throw new UnreachableException();
        }

        return expression.Type!;
    }




    public Type ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public Type ProcessSymbol(BoundSymbolExpression expression)
        => expression.Type!;




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
        return expression.Type!;
    }




    public Type ProcessCall(BoundCallExpression expression)
    {
        var calleeType = Process(expression.Callee);

        foreach (var argument in expression.Arguments)
            Process(argument);

        CheckCallExpression(expression, calleeType);

        // if the callee has not a function type, "expression.Type" will be null.
        // fallbacking this has no problem because this class reports diagnostics
        // if that's the case

        return expression.Type ?? Type.Void;
    }


    private void CheckCallExpression(BoundCallExpression expression, Type calleeType)
    {
        if (calleeType is not FunctionType functionType)
        {
            Report(Diagnostic.TypeCheckerCatalog.CannotCallNonFunction, location: expression.Source());
            return;
        }

        ReportIfArityDiffers(expression);
        MatchArgumentsTypeWithFunctionType(expression.Arguments, functionType);
    }


    private void MatchArgumentsTypeWithFunctionType(IReadOnlyList<BoundExpression> arguments, FunctionType functionType)
    {
        var parametersType = functionType.ParametersType;

        for (var i = 0; i < parametersType.Count && i < arguments.Count; i++)
            ReportIfDiffers(parametersType[i], arguments[i].Type!, arguments[i].Source());
    }




    public Type ProcessCast(BoundCastExpression expression)
    {
        Process(expression.Value);
        expression.Type = TypeFromNonVoidTypeName(expression.Syntax.Type);

        return expression.Type!;
    }

    #endregion








    #region Diagnostic Reporting

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
        // Here, although "void" should be reported, it is important to check whether
        // the type is a function type or not, since function types may return void, and
        // that's alright.

        if (!type.IsVoid || type is FunctionType)
            return;

        Report(Diagnostic.TypeCheckerCatalog.CannotUseVoidHere, location: location);
    }


    private void ReportIfArityDiffers(BoundCallExpression expression)
    {
        var functionType = (expression.Callee.Type as FunctionType)!;
        ReportIfArityDiffers(functionType.ParametersType.Count, expression.Arguments.Count, expression.Source());
    }


    private void ReportIfArityDiffers(int expected, int got, TokenLocation location)
    {
        if (expected == got)
            return;

        Report(Diagnostic.TypeCheckerCatalog.ArityDiffers, [expected, got], location);
    }


    private void ReportIfVoidFunctionReturnsValue(BoundReturnStatement statement)
    {
        if (_expectedReturnType!.IsVoid && statement.Expression is not null)
            Report(Diagnostic.TypeCheckerCatalog.FunctionCannotReturnValue, location: statement.Expression!.Source());
    }

    #endregion




    #region Type Convertors

    private Type TypeFromNonVoidTypeName(TypeName typeName)
    {
        var type = TypeFromTypeName(typeName);
        ReportIfVoid(type, typeName.BaseType);

        return type;
    }


    private Type TypeFromTypeName(TypeName typeName) => typeName switch
    {
        FunctionTypeName function => FunctionTypeFromTypeName(function),
        _ => RawTypeFromTypeName(typeName)
    };


    private Type RawTypeFromTypeName(TypeName typeName)
        => new Type(typeName.BaseType.TokenToPrimitive(), typeName.IsPointer);


    private FunctionType FunctionTypeFromTypeName(FunctionTypeName typeName)
    {
        var parameters = (from parameter in typeName.ParametersType select TypeFromTypeName(parameter)).ToArray();
        return new FunctionType(typeName.ReturnType.TokenToPrimitive(), parameters);
    }

    #endregion
}
