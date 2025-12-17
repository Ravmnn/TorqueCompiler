namespace Torque.Compiler;




public interface IBoundExpressionProcessor
{
    void Process(BoundExpression expression);

    void ProcessLiteral(BoundLiteralExpression expression);
    void ProcessBinary(BoundBinaryExpression expression);
    void ProcessUnary(BoundUnaryExpression expression);
    void ProcessGrouping(BoundGroupingExpression expression);
    void ProcessComparison(BoundComparisonExpression expression);
    void ProcessEquality(BoundEqualityExpression expression);
    void ProcessLogic(BoundLogicExpression expression);
    void ProcessSymbol(BoundSymbolExpression expression);
    void ProcessAssignment(BoundAssignmentExpression expression);
    void ProcessAssignmentReference(BoundAssignmentReferenceExpression expression);
    void ProcessPointerAccess(BoundPointerAccessExpression expression);
    void ProcessCall(BoundCallExpression expression);
    void ProcessCast(BoundCastExpression expression);

    void ProcessImplicitCast(BoundImplicitCastExpression expression);
}


public interface IBoundExpressionProcessor<out T>
{
    T Process(BoundExpression expression);

    T ProcessLiteral(BoundLiteralExpression expression);
    T ProcessBinary(BoundBinaryExpression expression);
    T ProcessUnary(BoundUnaryExpression expression);
    T ProcessGrouping(BoundGroupingExpression expression);
    T ProcessComparison(BoundComparisonExpression expression);
    T ProcessEquality(BoundEqualityExpression expression);
    T ProcessLogic(BoundLogicExpression expression);
    T ProcessSymbol(BoundSymbolExpression expression);
    T ProcessAssignment(BoundAssignmentExpression expression);
    T ProcessAssignmentReference(BoundAssignmentReferenceExpression expression);
    T ProcessPointerAccess(BoundPointerAccessExpression expression);
    T ProcessCall(BoundCallExpression expression);
    T ProcessCast(BoundCastExpression expression);

    T ProcessImplicitCast(BoundImplicitCastExpression expression);
}
