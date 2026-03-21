namespace Torque.Compiler.AST.Expressions;




public interface ISugarExpressionProcessor
{
    Expression Process(Expression expression);

    Expression ProcessNullptr(SugarNullptrExpression expression);
    Expression ProcessArrow(SugarArrowExpression expression);
}
