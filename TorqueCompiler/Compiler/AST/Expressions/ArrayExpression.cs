using System.Collections.Generic;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.Compiler.AST.Expressions;




public class ArrayExpression(TypeSyntax elementType, ulong size, IReadOnlyList<Expression>? elements, Span location) : Expression(location)
{
    public TypeSyntax ElementType { get; } = elementType;
    public ulong Size { get; } = size;
    public IReadOnlyList<Expression>? Elements { get; } = elements;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessArray(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessArray(this);
}
