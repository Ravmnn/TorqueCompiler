using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public class BoundIndexingExpression(IndexingExpression syntax, BoundExpression pointer, BoundExpression index) : BoundExpression(syntax)
{
    public new IndexingExpression Syntax => (base.Syntax as IndexingExpression)!;

    public BoundExpression Pointer { get; set; } = pointer;
    public BoundExpression Index { get; set; } = index;

    public override Type? Type => (Pointer.Type as PointerType)?.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessIndexing(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessIndexing(this);
}
