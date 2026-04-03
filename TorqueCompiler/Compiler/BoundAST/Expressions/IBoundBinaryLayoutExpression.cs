using Torque.Compiler.Types;


namespace Torque.Compiler.BoundAST.Expressions;




public interface IBoundBinaryLayoutExpression
{
    BoundExpression Left { get; set; }
    BoundExpression Right { get; set; }

    Type Type { get; set; }
}
