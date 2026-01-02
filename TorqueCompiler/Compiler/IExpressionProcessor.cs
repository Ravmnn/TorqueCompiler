namespace Torque.Compiler;




public interface IExpressionProcessor
{
    void Process(Expression expression);

    void ProcessLiteral(LiteralExpression expression);
    void ProcessBinary(BinaryExpression expression);
    void ProcessUnary(UnaryExpression expression);
    void ProcessGrouping(GroupingExpression expression);
    void ProcessComparison(ComparisonExpression expression);
    void ProcessEquality(EqualityExpression expression);
    void ProcessLogic(LogicExpression expression);
    void ProcessSymbol(SymbolExpression expression);
    void ProcessAssignment(AssignmentExpression expression);
    void ProcessPointerAccess(PointerAccessExpression expression);
    void ProcessCall(CallExpression expression);
    void ProcessCast(CastExpression expression);
    void ProcessArray(ArrayExpression expression);
    void ProcessIndexing(IndexingExpression expression);
}


public interface IExpressionProcessor<out T>
{
    T Process(Expression expression);

    T ProcessLiteral(LiteralExpression expression);
    T ProcessBinary(BinaryExpression expression);
    T ProcessUnary(UnaryExpression expression);
    T ProcessGrouping(GroupingExpression expression);
    T ProcessComparison(ComparisonExpression expression);
    T ProcessEquality(EqualityExpression expression);
    T ProcessLogic(LogicExpression expression);
    T ProcessSymbol(SymbolExpression expression);
    T ProcessAssignment(AssignmentExpression expression);
    T ProcessPointerAccess(PointerAccessExpression expression);
    T ProcessCall(CallExpression expression);
    T ProcessCast(CastExpression expression);
    T ProcessArray(ArrayExpression expression);
    T ProcessIndexing(IndexingExpression expression);
}
