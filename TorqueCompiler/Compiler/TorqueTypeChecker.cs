using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public enum ImplicitCastMode
{
    None, // implicit casts are disabled
    Safe, // only safe casts are performed (lower => higher, signed <=> signed, unsigned <=> unsigned)
    All   // all possible casts are performed (lower <=> higher, signed <=> unsigned)
}




public class TorqueTypeChecker(IReadOnlyList<BoundStatement> statements)
    : DiagnosticReporter<Diagnostic.TypeCheckerCatalog>, IBoundStatementProcessor, IBoundExpressionProcessor<Type>
{
    public const PrimitiveType DefaultLiteralIntegerType = PrimitiveType.Int32;
    public const PrimitiveType DefaultLiteralFloatType = PrimitiveType.Float32;




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
        => Process(statement.Expression);




    public void ProcessDeclaration(BoundDeclarationStatement statement)
    {
        var typeSyntax = statement.Syntax.Type;

        // the use of "let" is only allowed for function-scope variables
        var valueType = Process(statement.Value);
        var symbolType = typeSyntax.IsAuto ? valueType : TypeFromNonVoidTypeName(typeSyntax);

        statement.Symbol.Type = symbolType;
        statement.Value = ImplicitCastOrReport(symbolType, statement.Value, statement.Value.Location());
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
        => parameters.Select(parameter => TypeFromNonVoidTypeName(parameter.Type)).ToArray();


    private void SetFunctionSymbolParametersType(FunctionSymbol symbol, IReadOnlyList<Type> parametersType)
    {
        for (var i = 0; i < parametersType.Count; i++)
            symbol.Parameters[i].Type = parametersType[i];
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        if (statement.Expression is null)
            return;

        Process(statement.Expression);

        if (!_expectedReturnType!.IsVoid)
            statement.Expression = ImplicitCastOrReport(_expectedReturnType, statement.Expression, statement.Expression.Location());
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

        expression.Type = TypeOfLiteralToken(token);
        expression.Value = token.Value;

        return expression.Type!;
    }


    private PrimitiveType TypeOfLiteralToken(Token literal) => literal switch
    {
        _ when literal.IsBoolean() => PrimitiveType.Bool,
        _ when literal.IsChar() => PrimitiveType.Char,
        _ when literal.IsFloat() => DefaultLiteralFloatType,
        _ => DefaultLiteralIntegerType
    };




    public Type ProcessBinary(BoundBinaryExpression expression)
    {
        var leftType = Process(expression.Left);
        Process(expression.Right);

        expression.Right = ImplicitCastOrReport(leftType, expression.Right, expression.Right.Location());

        return expression.Type!;
    }




    public Type ProcessUnary(BoundUnaryExpression expression)
    {
        Process(expression.Expression);

        switch (expression.Syntax.Operator.Type)
        {
            case TokenType.Minus: break;
            case TokenType.Exclamation:
                expression.Expression = ImplicitCastOrReport(PrimitiveType.Bool, expression.Expression, expression.Location());
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

        expression.Right = ImplicitCastOrReport(leftType, expression.Right, expression.Right.Location());
        return expression.Type;
    }


    public Type ProcessEquality(BoundEqualityExpression expression)
    {
        var leftType = Process(expression.Left);
        Process(expression.Right);

        expression.Right = ImplicitCastOrReport(leftType, expression.Right, expression.Right.Location());

        return expression.Type;
    }


    public Type ProcessLogic(BoundLogicExpression expression)
    {
        Process(expression.Left);
        Process(expression.Right);

        expression.Left = ImplicitCastOrReport(PrimitiveType.Bool, expression.Left, expression.Right.Location());
        expression.Right = ImplicitCastOrReport(PrimitiveType.Bool, expression.Right, expression.Right.Location());

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

        expression.Value = ImplicitCastOrReport(referenceType, expression.Value, expression.Value.Location());

        return expression.Type!;
    }


    public Type ProcessAssignmentReference(BoundAssignmentReferenceExpression expression)
        => Process(expression.Reference);




    public Type ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        var type = Process(expression.Pointer);
        ReportIfNotAPointer(type, expression.Pointer.Location());

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
            Report(Diagnostic.TypeCheckerCatalog.CannotCallNonFunction, location: expression.Location());
            return;
        }

        ReportIfArityDiffers(expression);
        MatchArgumentsTypeWithFunctionType(expression.Arguments, functionType);
    }


    private void MatchArgumentsTypeWithFunctionType(IList<BoundExpression> arguments, FunctionType functionType)
    {
        var parametersType = functionType.ParametersType;

        for (var i = 0; i < parametersType.Count && i < arguments.Count; i++)
            arguments[i] = ImplicitCastOrReport(parametersType[i], arguments[i], arguments[i].Location());
    }




    public Type ProcessCast(BoundCastExpression expression)
    {
        var type = Process(expression.Value);
        ReportIfVoidExpression(type, expression.Value.Location());

        expression.Type = TypeFromNonVoidTypeName(expression.Syntax.Type);

        return expression.Type!;
    }




    public Type ProcessImplicitCast(BoundImplicitCastExpression expression)
        => throw new UnreachableException();




    public Type ProcessArray(BoundArrayExpression expression)
    {
        // TODO: cannot have an array with size 0

        var elementType = TypeFromTypeName(expression.Syntax.ElementType);
        expression.Type = new ArrayType(elementType, expression.Syntax.Size);

        if (expression.Elements is not null)
            MatchElementTypes(expression.Elements, elementType, expression.Syntax);

        return expression.Type;
    }


    private void MatchElementTypes(IList<BoundExpression> elements, Type elementType, ArrayExpression syntax)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];

            Process(element);
            elements[i] = ImplicitCastOrReport(elementType, element, syntax.Source());
        }
    }




    public Type ProcessIndexing(BoundIndexingExpression expression)
    {
        Process(expression.Pointer);
        Process(expression.Index);

        ReportIfNotAPointer(expression.Pointer.Type!, expression.Pointer.Location());
        expression.Index = ImplicitCastOrReport(PrimitiveType.Int64, expression.Index, expression.Index.Location());

        return expression.Type!;
    }




    public Type ProcessDefault(BoundDefaultExpression expression)
    {
        expression.Type = TypeFromNonVoidTypeName(expression.Syntax.TypeName);
        return expression.Type;
    }

    #endregion








    #region Diagnostic Reporting

    private BoundExpression ImplicitCastOrReport(Type expected, BoundExpression expression, SourceLocation location)
    {
        // here, "expression" should already have been processed (typed)

        var got = expression.Type!;
        var gotLiteral = expression is BoundLiteralExpression;

        if (expected == got)
            return expression;

        if (TryImplicitCast(got, expected, gotLiteral) is { } type)
            return new BoundImplicitCastExpression(expression, type);

        if (!ReportIfVoidExpression(got, location))
            Report(Diagnostic.TypeCheckerCatalog.TypeDiffers, [expected.ToString(), got.ToString()], location);

        return expression;
    }


    private bool ReportIfNotAPointer(Type type, SourceLocation location)
    {
        if (type.IsPointer)
            return false;

        Report(Diagnostic.TypeCheckerCatalog.PointerExpected, location: location);
        return true;
    }


    private bool ReportIfVoidTypeName(Type type, SourceLocation location)
    {
        // Here, although "void" should be reported, it is important to check whether
        // the type is a function type or not, since function types may return void, and
        // that's alright.

        if (!type.IsVoid || type is FunctionType)
            return false;

        Report(Diagnostic.TypeCheckerCatalog.CannotUseVoidHere, location: location);
        return true;
    }


    private bool ReportIfVoidExpression(Type type, SourceLocation location)
    {
        if (!type.IsVoid || type is FunctionType)
            return false;

        Report(Diagnostic.TypeCheckerCatalog.ExpressionDoesNotReturnAnyValue, location: location);
        return true;
    }


    private bool ReportIfArityDiffers(BoundCallExpression expression)
    {
        var functionType = (expression.Callee.Type as FunctionType)!;
        return ReportIfArityDiffers(functionType.ParametersType.Count, expression.Arguments.Count, expression.Location());
    }


    private bool ReportIfArityDiffers(int expected, int got, SourceLocation location)
    {
        if (expected == got)
            return false;

        Report(Diagnostic.TypeCheckerCatalog.ArityDiffers, [expected, got], location);
        return true;
    }

    #endregion




    #region Implicit Casting

    private Type? TryImplicitCast(Type from, Type to, bool gotLiteral = false)
    {
        if (!CanImplicitCast(from, to, gotLiteral))
            return null;

        return to;
    }


    private bool CanImplicitCast(Type from, Type to, bool gotLiteral = false)
    {
        var allCasts = ImplicitCastMode == ImplicitCastMode.All;
        var noCasts = ImplicitCastMode == ImplicitCastMode.None;

        if (noCasts)
            return false;

        var sameTypes = from == to;
        var bothBase = from.IsBase && to.IsBase;
        var anyIsAuto = from.IsAuto || to.IsAuto;

        if (sameTypes || anyIsAuto)
            return true;

        if (!bothBase)
            return false;

        // if the value is a literal (char included), implicit cast can be forced
        if (allCasts || gotLiteral)
            return true;

        var signDiffers = from.IsSigned != to.IsSigned;
        var floatToInt = from.IsFloat && !to.IsFloat; // float to int may result in loss of data
        var sourceBigger = from.Base.Type.SizeOfThisInMemory() > to.Base.Type.SizeOfThisInMemory();

        if (signDiffers || floatToInt || sourceBigger)
            return false;

        return true;
    }

    #endregion




    #region Type Convertors

    private Type TypeFromNonVoidTypeName(TypeName typeName)
    {
        var type = TypeFromTypeName(typeName);
        ReportIfVoidTypeName(type, typeName.Base.TypeToken.Location);

        return type;
    }




    private Type TypeFromTypeName(TypeName typeName) => typeName switch
    {
        BaseTypeName baseTypeName => TypeFromBaseTypeName(baseTypeName),

        // all of the types above are descendant from "PointerTypeName", so it's necessary to first
        // check the most derivative first
        FunctionTypeName functionTypeName => FunctionTypeFromTypeName(functionTypeName),
        PointerTypeName pointerTypeName => TypeFromPointerTypeName(pointerTypeName),

        _ => throw new UnreachableException()
    };


    private BaseType TypeFromBaseTypeName(BaseTypeName typeName)
    {
        if (typeName.IsAuto)
            Report(Diagnostic.TypeCheckerCatalog.CannotUseLetHere, location: typeName.TypeToken);

        return new BaseType(typeName.Base.TypeToken.TokenToPrimitive());
    }


    private PointerType TypeFromPointerTypeName(PointerTypeName pointerTypeName)
        => new PointerType(TypeFromTypeName(pointerTypeName.Type));


    private FunctionType FunctionTypeFromTypeName(FunctionTypeName typeName)
    {
        var parametersType = typeName.ParametersType.Select(TypeFromTypeName).ToArray();
        var returnType = TypeFromTypeName(typeName.ReturnType);

        return new FunctionType(returnType, parametersType);
    }

    #endregion
}
