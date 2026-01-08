using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundCallExpression(CallExpression syntax, BoundExpression callee, IReadOnlyList<BoundExpression> arguments)
    : BoundExpression(syntax)
{
    public new CallExpression Syntax => (base.Syntax as CallExpression)!;

    public BoundExpression Callee { get; set; } = callee;
    public IList<BoundExpression> Arguments { get; } = arguments.ToList();

    public override Type? Type => (Callee.Type as FunctionType)?.ReturnType;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessCall(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessCall(this);
}
