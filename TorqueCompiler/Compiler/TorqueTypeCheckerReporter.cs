using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


using Type = Torque.Compiler.Types.Type;


namespace Torque.Compiler;




public sealed class TorqueTypeCheckerReporter(TorqueTypeChecker typeChecker) : DiagnosticReporter<TypeCheckerCatalog>,
    IBoundExpressionProcessor, IBoundStatementProcessor,
    IBoundDeclarationProcessor
{
    public TorqueTypeChecker TypeChecker { get; } = typeChecker;




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




    public void ProcessVariableDefinition(BoundVariableDeclarationStatement statement)
    {

    }




    public void ProcessFunctionDefinition(BoundFunctionDeclarationStatement statement)
    {

    }




    private void ValidateFunctionBody(BoundFunctionDeclarationStatement statement)
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
    {

    }




    public void ProcessBreak(BoundBreakStatement statement)
    {

    }








    public void Process(BoundExpression expression)
    {
        ReportIfVoidExpression(expression.Type!, expression.Location);
    }




    public void ProcessLiteral(BoundLiteralExpression expression)
    { }




    public void ProcessBinary(BoundBinaryExpression expression)
    { }




    public void ProcessUnary(BoundUnaryExpression expression)
    { }




    public void ProcessGrouping(BoundGroupingExpression expression)
    { }




    public void ProcessComparison(BoundComparisonExpression expression)
    { }




    public void ProcessEquality(BoundEqualityExpression expression)
    { }




    public void ProcessLogic(BoundLogicExpression expression)
    { }




    public void ProcessSymbol(BoundSymbolExpression expression)
    {

    }




    public void ProcessAddress(BoundAddressExpression expression)
    {

    }


    public void ProcessAddressable(BoundAddressableExpression expression)
    {

    }




    public void ProcessAssignment(BoundAssignmentExpression expression)
    {

    }


    public void ProcessAssignmentReference(BoundAssignmentReferenceExpression expression)
    {

    }




    public void ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        ReportIfNotAPointer(expression.Type!, expression.Pointer.Location);
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

    }


    public void ProcessImplicitCast(BoundImplicitCastExpression expression)
    {

    }




    public void ProcessArray(BoundArrayExpression expression)
    {
        if (expression.Syntax.Length == 0)
            Report(TypeCheckerCatalog.CannotHaveAZeroSizedArray, location: expression.Location);
    }




    public void ProcessIndexing(BoundIndexingExpression expression)
    {
        ReportIfNotAPointer(expression.Pointer.Type!, expression.Pointer.Location);
    }




    public void ProcessDefault(BoundDefaultExpression expression)
    {

    }




    public void ProcessStruct(BoundStructExpression expression)
    {

    }

    public void ProcessMemberAccess(BoundMemberAccessExpression expression)
    {
        var structType = (expression.Type as StructType)!;

        if (!expression.Compound.Type!.IsStruct)
        {
            Report(TypeCheckerCatalog.StructExpected, location: expression.Location);
            return;
        }

        if (structType.GetField(expression.Member.Name) is null)
            Report(TypeCheckerCatalog.UndeclaredStructMember, [expression.Member.Name, structType.Name.Name], expression.Location);
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




    public void ReportTypeDiffers(Type expected, Type got, Span location)
        => Report(TypeCheckerCatalog.TypeDiffers, [expected.ToString(), got.ToString()], location);
}
