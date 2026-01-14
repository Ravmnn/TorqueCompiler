using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler;




public enum ImplicitCastMode
{
    None, // implicit casts are disabled
    Safe, // only safe casts are performed (lower => higher, signed <=> signed, unsigned <=> unsigned)
    All   // all possible casts are performed (lower <=> higher, signed <=> unsigned)
}




public class TorqueTypeChecker(IReadOnlyList<BoundStatement> statements)
    : DiagnosticReporter<TypeCheckerCatalog>, IBoundStatementProcessor, IBoundExpressionProcessor<Type>
{
    public const PrimitiveType DefaultLiteralIntegerType = PrimitiveType.Int32;
    public const PrimitiveType DefaultLiteralFloatType = PrimitiveType.Float32;




    private bool _acceptVoidExpressions;


    private Type? _expectedReturnType;


    public IReadOnlyList<BoundStatement> Statements { get; } = statements;

    public ImplicitCastMode ImplicitCastMode { get; set; } = ImplicitCastMode.Safe;




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
    {
        _acceptVoidExpressions = true;
        Process(statement.Expression);
        _acceptVoidExpressions = false;
    }


    public void ProcessDeclaration(BoundDeclarationStatement statement)
    {
        var typeSyntax = statement.Syntax.Type;

        // the use of "let" is only allowed for function-scope variables
        var valueType = Process(statement.Value);
        var symbolType = typeSyntax.IsAuto ? valueType : TypeFromNonVoidTypeName(typeSyntax);

        statement.Symbol.Type = symbolType;
        statement.Value = ImplicitCastOrReport(symbolType, statement.Value, true);
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
    {
        var returnType = TypeFromTypeName(statement.Syntax.ReturnType);
        var parametersType = ParametersTypeFromParametersDeclaration(statement.Syntax.Parameters);

        SetFunctionAndParametersSymbolsType(statement.Symbol, returnType, parametersType);

        _expectedReturnType = returnType;
        Process(statement.Body);
        _expectedReturnType = null;
    }


    private void SetFunctionAndParametersSymbolsType(FunctionSymbol symbol, Type returnType, IReadOnlyList<Type> parametersType)
    {
        symbol.Type = new FunctionType(returnType, parametersType);
        SetFunctionSymbolParametersType(symbol, parametersType);
    }


    private IReadOnlyList<Type> ParametersTypeFromParametersDeclaration(IReadOnlyList<FunctionParameterDeclaration> parameters)
        => parameters.Select(parameter => TypeFromNonVoidTypeName(parameter.Type)).ToArray();


    private void SetFunctionSymbolParametersType(FunctionSymbol symbol, IReadOnlyList<Type> parametersType)
    {
        for (var i = 0; i < parametersType.Count; i++)
            symbol.Parameters[i].Type = parametersType[i];
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        if (statement.Expression is null)
        {
            ReportIfExpectedTypeIsNotVoidAndDoesNotReturn(statement.Location);
            return;
        }

        ProcessReturnValue(statement);
    }


    private void ProcessReturnValue(BoundReturnStatement statement)
    {
        Process(statement.Expression!);

        if (!_expectedReturnType!.IsVoid)
            statement.Expression = ImplicitCastOrReport(_expectedReturnType, statement.Expression!);
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }




    public void ProcessIf(BoundIfStatement statement)
    {
        Process(statement.Condition);
        statement.Condition = ImplicitCastOrReport(PrimitiveType.Bool, statement.Condition);

        Process(statement.ThenStatement);

        if (statement.ElseStatement is not null)
            Process(statement.ElseStatement);
    }

    #endregion








    #region Expression

    public Type Process(BoundExpression expression)
    {
        var type = expression.Process(this);
        ReportIfVoidExpression(type, expression.Location);

        return type;
    }


    public void ProcessAll(IReadOnlyList<BoundExpression> expressions)
    {
        foreach (var expression in expressions)
            Process(expression);
    }




    public Type ProcessLiteral(BoundLiteralExpression expression)
    {
        expression.Value = expression.Syntax.Value;
        expression.Type = TypeOfLiteralObject(expression.Value);

        return expression.Type;
    }


    private PrimitiveType TypeOfLiteralObject(object literal) => literal switch
    {
        bool => PrimitiveType.Bool,
        byte => PrimitiveType.Char,
        double => DefaultLiteralFloatType,
        ulong => DefaultLiteralIntegerType,

        _ => throw new UnreachableException()
    };




    public Type ProcessBinary(BoundBinaryExpression expression)
    {
        var leftType = Process(expression.Left);
        var rightType = Process(expression.Right);

        ImplicitCastLeftOrRightBinaryOperand(expression, leftType, rightType);

        return expression.Type!;
    }


    private void ImplicitCastLeftOrRightBinaryOperand(BoundBinaryExpression expression, Type leftType, Type rightType)
    {
        var rightOperandCast = TryImplicitCast(leftType, expression.Right);

        if (rightOperandCast is not null)
            expression.Right = rightOperandCast;
        else
            expression.Left = ImplicitCastOrReport(rightType, expression.Left);
    }




    public Type ProcessUnary(BoundUnaryExpression expression)
    {
        Process(expression.Expression);

        switch (expression.Syntax.Operator)
        {
            case TokenType.Minus: break;
            case TokenType.Exclamation:
                expression.Expression = ImplicitCastOrReport(PrimitiveType.Bool, expression.Expression);
                break;

            default: throw new UnreachableException();
        }

        return expression.Type!;
    }




    public Type ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public Type ProcessComparison(BoundComparisonExpression expression)
    {
        // TODO: when compound types are supported, check that the expression here is a primitive (compounds cannot be compared, same for equality)

        var leftType = Process(expression.Left);
        Process(expression.Right);

        expression.Right = ImplicitCastOrReport(leftType, expression.Right);
        return expression.Type;
    }


    public Type ProcessEquality(BoundEqualityExpression expression)
    {
        var leftType = Process(expression.Left);
        Process(expression.Right);

        expression.Right = ImplicitCastOrReport(leftType, expression.Right);

        return expression.Type;
    }


    public Type ProcessLogic(BoundLogicExpression expression)
    {
        Process(expression.Left);
        Process(expression.Right);

        expression.Left = ImplicitCastOrReport(PrimitiveType.Bool, expression.Left);
        expression.Right = ImplicitCastOrReport(PrimitiveType.Bool, expression.Right);

        return expression.Type;
    }




    public Type ProcessSymbol(BoundSymbolExpression expression)
        => expression.Type;




    public Type ProcessAddress(BoundAddressExpression expression)
    {
        Process(expression.Expression);
        return expression.Type;
    }


    public Type ProcessAddressable(BoundAddressableExpression expression)
    {
        Process(expression.Expression);
        return expression.Type!;
    }




    public Type ProcessAssignment(BoundAssignmentExpression expression)
    {
        var referenceType = Process(expression.Reference);
        Process(expression.Value);

        expression.Value = ImplicitCastOrReport(referenceType, expression.Value);

        return expression.Type!;
    }


    public Type ProcessAssignmentReference(BoundAssignmentReferenceExpression expression)
        => Process(expression.Reference);




    public Type ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        var type = Process(expression.Pointer);
        ReportIfNotAPointer(type, expression.Pointer.Location);

        return expression.Type!;
    }




    public Type ProcessCall(BoundCallExpression expression)
    {
        Process(expression.Callee);
        ProcessAll(expression.Arguments.ToArray());

        CheckCallExpression(expression);

        // if the callee has not a function type, "expression.Type" will be null.
        // fall backing this has no problem because this class reports diagnostics
        // if that's the case

        return expression.Type ?? Type.Void;
    }


    private void CheckCallExpression(BoundCallExpression expression)
    {
        if (expression.Callee.Type is not FunctionType functionType)
        {
            Report(TypeCheckerCatalog.CannotCallNonFunction, location: expression.Location);
            return;
        }

        ReportIfArityDiffers(expression);
        MatchArgumentsTypeWithFunctionType(expression.Arguments, functionType);
    }


    private void MatchArgumentsTypeWithFunctionType(IList<BoundExpression> arguments, FunctionType functionType)
    {
        var parametersType = functionType.ParametersType;

        for (var i = 0; i < parametersType.Count && i < arguments.Count; i++)
            arguments[i] = ImplicitCastOrReport(parametersType[i], arguments[i]);
    }




    public Type ProcessCast(BoundCastExpression expression)
    {
        Process(expression.Value);
        expression.Type = TypeFromNonVoidTypeName(expression.Syntax.Type);

        return expression.Type!;
    }




    public Type ProcessImplicitCast(BoundImplicitCastExpression expression)
        => throw new UnreachableException();




    public Type ProcessArray(BoundArrayExpression expression)
    {
        var elementType = TypeFromTypeName(expression.Syntax.ElementType);

        expression.ArrayType = new ArrayType(elementType, expression.Syntax.Length); // this is the type used to the alloca
        expression.Type = new PointerType(elementType); // to avoid any future hidden bug, force the use of the pointer type

        CheckArrayExpression(expression, elementType);

        return expression.Type;
    }


    private void CheckArrayExpression(BoundArrayExpression expression, Type elementType)
    {
        if (expression.Syntax.Length == 0)
            Report(TypeCheckerCatalog.CannotHaveAZeroSizedArray, location: expression.Location);

        if (expression.Elements is not null)
            MatchElementTypes(expression.Elements, elementType);
    }


    private void MatchElementTypes(IList<BoundExpression> elements, Type elementType)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];

            Process(element);
            elements[i] = ImplicitCastOrReport(elementType, element);
        }
    }




    public Type ProcessIndexing(BoundIndexingExpression expression)
    {
        Process(expression.Pointer);
        Process(expression.Index);

        ReportIfNotAPointer(expression.Pointer.Type!, expression.Pointer.Location);
        expression.Index = ImplicitCastOrReport(PrimitiveType.Int64, expression.Index);

        return expression.Type!;
    }




    public Type ProcessDefault(BoundDefaultExpression expression)
    {
        expression.Type = TypeFromNonVoidTypeName(expression.Syntax.TypeSyntax);
        return expression.Type;
    }

    #endregion








    #region Diagnostic Reporting

    private BoundExpression ImplicitCastOrReport(Type expected, BoundExpression expression, bool forceIfLiteral = false)
    {
        if (TryImplicitCast(expected, expression, forceIfLiteral) is { } result)
            return result;

        Report(TypeCheckerCatalog.TypeDiffers, [expected.ToString(), expression.Type!.ToString()], expression.Location);
        return expression;
    }


    private BoundExpression? TryImplicitCast(Type expected, BoundExpression expression, bool forceIfLiteral = false)
    {
        // here, "expression" should already have been processed (typed)

        var got = expression.Type!;

        if (expected == got)
            return expression;

        var forceForBaseTypes = expression is BoundLiteralExpression && forceIfLiteral;

        if (TryImplicitCast(got, expected, forceForBaseTypes) is { } type)
            return new BoundImplicitCastExpression(expression, type);

        return null;
    }




    private bool ReportIfNotAPointer(Type type, Span location)
    {
        if (type.IsPointer)
            return false;

        Report(TypeCheckerCatalog.PointerExpected, location: location);
        return true;
    }


    private bool ReportIfVoidTypeName(Type type, Span location)
    {
        // Here, although "void" should be reported, it is important to check whether
        // the type is a function type or not, since function types may return void, and
        // that's alright.

        if (!type.IsVoid || type is FunctionType)
            return false;

        Report(TypeCheckerCatalog.CannotUseVoidHere, location: location);
        return true;
    }


    private bool ReportIfVoidExpression(Type type, Span location)
    {
        if (_acceptVoidExpressions || !type.IsVoid || type is FunctionType)
            return false;

        Report(TypeCheckerCatalog.ExpressionDoesNotReturnAnyValue, location: location);
        return true;
    }


    private bool ReportIfArityDiffers(BoundCallExpression expression)
    {
        var functionType = (expression.Callee.Type as FunctionType)!;
        return ReportIfArityDiffers(functionType.ParametersType.Count, expression.Arguments.Count, expression.Location);
    }


    private bool ReportIfArityDiffers(int expected, int got, Span location)
    {
        if (expected == got)
            return false;

        Report(TypeCheckerCatalog.ArityDiffers, [expected, got], location);
        return true;
    }


    private bool ReportIfExpectedTypeIsNotVoidAndDoesNotReturn(Span location)
    {
        if (_expectedReturnType!.IsVoid)
            return false;

        Report(TypeCheckerCatalog.ExpectedAReturnValue, location: location);
        return true;
    }

    #endregion




    #region Implicit Casting

    private Type? TryImplicitCast(Type from, Type to, bool forceForBaseTypes = false)
    {
        if (!CanImplicitCast(from, to, forceForBaseTypes))
            return null;

        return to;
    }


    private bool CanImplicitCast(Type from, Type to, bool forceForBaseTypes = false)
    {
        var allCasts = ImplicitCastMode == ImplicitCastMode.All;
        var noCasts = ImplicitCastMode == ImplicitCastMode.None;

        var sameTypes = from == to;
        var bothBase = from.IsBase && to.IsBase;
        var anyIsAuto = from.IsAuto || to.IsAuto;

        if (allCasts || sameTypes || anyIsAuto)
            return true;

        if (bothBase && forceForBaseTypes)
            return true;

        if (noCasts || !bothBase)
            return false;

        var signDiffers = from.IsSigned != to.IsSigned;
        var floatToInt = from.IsFloat && to.IsInteger; // float to int may result in loss of data
        var sourceBigger = from.SizeOfTypeInMemory() > to.SizeOfTypeInMemory();

        if (signDiffers || floatToInt || sourceBigger)
            return false;

        return true;
    }

    #endregion




    #region Type Convertors

    private Type TypeFromNonVoidTypeName(TypeSyntax typeSyntax)
    {
        var type = TypeFromTypeName(typeSyntax);
        ReportIfVoidTypeName(type, typeSyntax.BaseType.TypeSymbol.Location);

        return type;
    }




    private Type TypeFromTypeName(TypeSyntax typeSyntax) => typeSyntax switch
    {
        BaseTypeSyntax baseTypeName => TypeFromBaseTypeName(baseTypeName),

        // all of the types above are descendant from "PointerTypeName", so it's necessary to first
        // check the most derivative first
        FunctionTypeSyntax functionTypeName => FunctionTypeFromTypeName(functionTypeName),
        PointerTypeSyntax pointerTypeName => TypeFromPointerTypeName(pointerTypeName),

        _ => throw new UnreachableException()
    };


    private BaseType TypeFromBaseTypeName(BaseTypeSyntax typeSyntax)
    {
        if (typeSyntax.IsAuto)
            Report(TypeCheckerCatalog.CannotUseLetHere, location: typeSyntax.TypeSymbol.Location);

        return new BaseType(typeSyntax.BaseType.TypeSymbol.SymbolToPrimitiveType());
    }


    private PointerType TypeFromPointerTypeName(PointerTypeSyntax pointerTypeSyntax)
        => new PointerType(TypeFromTypeName(pointerTypeSyntax.InnerType));


    private FunctionType FunctionTypeFromTypeName(FunctionTypeSyntax typeSyntax)
    {
        var parametersType = typeSyntax.ParametersType.Select(TypeFromTypeName).ToArray();
        var returnType = TypeFromTypeName(typeSyntax.ReturnType);

        return new FunctionType(returnType, parametersType);
    }

    #endregion
}
