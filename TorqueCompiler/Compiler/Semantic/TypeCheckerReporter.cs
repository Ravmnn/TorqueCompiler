using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


using Type = Torque.Compiler.Types.Type;


namespace Torque.Compiler.Semantic;




public sealed class TypeCheckerReporter(TypeChecker typeChecker) : DiagnosticReporter<TypeCheckerCatalog>,
    IBoundExpressionProcessor, IBoundStatementProcessor,
    IBoundDeclarationProcessor
{
    public TypeChecker TypeChecker { get; } = typeChecker;




    public void Process(IBoundDeclaration declaration)
    {
        declaration.ProcessDeclaration(this);
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement declaration)
    { }








    public void Process(BoundStatement statement)
    {
        statement.Process(this);
    }




    public void ProcessExpression(BoundExpressionStatement statement)
    { }




    public void ProcessVariable(BoundVariableDeclarationStatement statement)
    {

    }




    public void ProcessFunction(BoundFunctionDeclarationStatement statement)
    {

    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        if (statement.Expression is null)
            ReportIfExpectedTypeIsNotVoidAndDoesNotReturn(statement.Location);
    }




    public void ProcessBlock(BoundBlockStatement statement)
    { }




    public void ProcessIf(BoundIfStatement statement)
    { }




    public void ProcessWhile(BoundWhileStatement statement)
    { }


    public void ProcessContinue(BoundContinueStatement statement)
    { }


    public void ProcessBreak(BoundBreakStatement statement)
    { }








    public void Process(BoundExpression expression)
    {
        if (AnyOperandsOfBinaryHasErrorType(expression) || UnaryInnerExpressionHasErrorType(expression))
            return;

        ReportIfVoidExpression(expression.Type, expression.Location);
        expression.Process(this);
    }




    public void ProcessLiteral(BoundLiteralExpression expression)
    { }




    public void ProcessBinary(BoundBinaryExpression expression)
    {
        ReportExpressionMustHaveNumericOperandsIfError(expression);
    }




    public void ProcessUnary(BoundUnaryExpression expression)
    {
        if (expression.Syntax.Operator == TokenType.Minus)
            ReportExpressionMustHaveNumericOperandsIfError(expression);
    }




    public void ProcessGrouping(BoundGroupingExpression expression)
    { }




    public void ProcessComparison(BoundComparisonExpression expression)
    {
        ReportExpressionMustHaveNumericOperandsIfError(expression);
    }




    public void ProcessEquality(BoundEqualityExpression expression)
    {
        ReportIfTypeDiffers(expression.Left.Type, expression.Right.Type, expression.Location);
    }




    public void ProcessLogic(BoundLogicExpression expression)
    { }




    public void ProcessSymbol(BoundSymbolExpression expression)
    { }




    public void ProcessAddress(BoundAddressExpression expression)
    { }




    public void ProcessAssignment(BoundAssignmentExpression expression)
    { }




    public void ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        ReportIfNotAPointer(expression.Pointer.Type, expression.Pointer.Location);
    }




    public void ProcessCall(BoundCallExpression expression)
    {
        if (expression.Callee.Type is not FunctionType)
        {
            Report(TypeCheckerCatalog.CannotCallNonFunction, location: expression.Location);
            return;
        }

        ReportIfArityDiffers(expression);
    }




    public void ProcessCast(BoundCastExpression expression)
    {
        if (expression.Value.Type.IsStruct || expression.Type.IsStruct)
            Report(TypeCheckerCatalog.CannotCastBetweenStructs, location: expression.Location);
    }


    public void ProcessImplicitCast(BoundImplicitCastExpression expression)
    { }




    public void ProcessArray(BoundArrayExpression expression)
    {
        if (expression.Syntax.Length == 0)
            Report(TypeCheckerCatalog.CannotHaveAZeroSizedArray, location: expression.Location);
    }




    public void ProcessIndexing(BoundIndexingExpression expression)
    {
        if (expression.Pointer.Type is not null)
            ReportIfNotAPointer(expression.Pointer.Type, expression.Pointer.Location);
    }




    public void ProcessDefault(BoundDefaultExpression expression)
    { }




    public void ProcessStruct(BoundStructExpression expression)
    {
        var structType = (expression.Type as StructType)!;

        foreach (var initialization in expression.InitializationList)
            ReportIfStructHasNotField(structType, initialization.Member.Name, initialization.Member.Location);
    }

    public void ProcessMemberAccess(BoundMemberAccessExpression expression)
    {
        var structType = (expression.Compound.Type as StructType)!;

        if (ReportIfNotAStruct(structType, expression.Compound.Location))
            return;

        ReportIfStructHasNotField(structType, expression.Member.Name, expression.Location);
    }




    public bool ReportIfStructHasNotField(StructType structType, string field, Span location)
    {
        if (structType.GetField(field) is not null)
            return false;

        Report(TypeCheckerCatalog.UndeclaredStructMember, [field, structType.Name.Name], location);
        return true;
    }


    public bool ReportIfNotAPointer(Type type, Span location)
    {
        if (type.IsPointer)
            return false;

        Report(TypeCheckerCatalog.PointerExpected, location: location);
        return true;
    }


    public bool ReportIfVoidTypeName(Type type, Span location)
    {
        // Here, although "void" should be reported, it is important to check whether
        // the type is a function type or not, since function types may return void, and
        // that's alright.

        if (!type.IsVoid || type is FunctionType)
            return false;

        Report(TypeCheckerCatalog.CannotUseVoidHere, location: location);
        return true;
    }


    public bool ReportIfVoidExpression(Type type, Span location)
    {
        if (TypeChecker.AcceptVoidExpressions || !type.IsVoid || type is FunctionType)
            return false;

        Report(TypeCheckerCatalog.ExpressionDoesNotReturnAnyValue, location: location);
        return true;
    }


    public bool ReportIfArityDiffers(BoundCallExpression expression)
    {
        var functionType = (expression.Callee.Type as FunctionType)!;
        return ReportIfArityDiffers(functionType.ParametersType.Count, expression.Arguments.Count, expression.Location);
    }


    public bool ReportIfArityDiffers(int expected, int got, Span location)
    {
        if (expected == got)
            return false;

        Report(TypeCheckerCatalog.ArityDiffers, [expected, got], location);
        return true;
    }


    public bool ReportIfExpectedTypeIsNotVoidAndDoesNotReturn(Span location)
    {
        if (TypeChecker.ExpectedReturnType!.IsVoid)
            return false;

        Report(TypeCheckerCatalog.ExpectedAReturnValue, location: location);
        return true;
    }


    public bool ReportStructIncompatibleIfExpressionIsStruct(BoundExpression expression)
    {
        if (!expression.Type.IsStruct)
            return false;

        Report(TypeCheckerCatalog.ExpressionIncompatibleWithStructs, location: expression.Location);
        return true;
    }


    public bool ReportExpressionMustHaveNumericOperandsIfError(BoundExpression expression)
    {
        if (!expression.Type.IsError)
            return false;

        Report(TypeCheckerCatalog.ExpressionMustHaveNumericOperands, location: expression.Location);
        return true;
    }


    public bool ReportIfTypeDiffers(Type expected, Type got, Span location)
    {
        if (expected == got)
            return false;

        ReportTypeDiffers(expected, got, location);
        return true;
    }


    public bool ReportIfNotAStruct(Type type, Span location)
    {
        if (type.IsStruct)
            return false;

        Report(TypeCheckerCatalog.StructExpected, location: location);
        return true;
    }





    public void ReportTypeDiffers(Type expected, Type got, Span location)
        => Report(TypeCheckerCatalog.TypeDiffers, [expected.ToString(), got.ToString()], location);




    private bool UnaryInnerExpressionHasErrorType(BoundExpression expression)
    {
        if (expression is BoundUnaryExpression unary)
            return AnyExpressionHasErrorType(unary.Expression);

        return false;
    }


    private bool AnyOperandsOfBinaryHasErrorType(BoundExpression expression)
    {
        if (expression is IBoundBinaryLayoutExpression binary)
            return AnyExpressionHasErrorType(binary.Left, binary.Right);

        return false;
    }


    private bool AnyExpressionHasErrorType(params IReadOnlyList<BoundExpression> expressions)
        => expressions.Any(expression => expression.Type.IsError);
}
