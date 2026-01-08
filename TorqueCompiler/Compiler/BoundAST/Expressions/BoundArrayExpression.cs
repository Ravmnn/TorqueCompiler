using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundArrayExpression(ArrayExpression syntax, IReadOnlyList<BoundExpression>? elements) : BoundExpression(syntax)
{
    public new ArrayExpression Syntax => (base.Syntax as ArrayExpression)!;

    public IList<BoundExpression>? Elements { get; } = elements?.ToList();
    public Type? ArrayType { get; set; }




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessArray(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessArray(this);
}
