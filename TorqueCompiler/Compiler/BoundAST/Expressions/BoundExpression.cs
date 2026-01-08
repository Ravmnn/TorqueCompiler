using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.BoundAST.Expressions;




public abstract class BoundExpression(Expression syntax)
{
    public Expression Syntax { get; } = syntax;
    public Span Location => Syntax.Location;

    public virtual Type? Type { get; set; }




    public abstract void Process(IBoundExpressionProcessor processor);
    public abstract T Process<T>(IBoundExpressionProcessor<T> processor);
}

// An addressable expression is an expression in which it is possible to get its memory address

// An assignment reference is a memory address (pointer) that will only be used to access the location
// in memory of something to modify its value and cannot be used to get the value inside that location (write-only).
// I was trying to find a way to make the AssignmentExpression work for anything that had
// a valid memory address and could be modified. The SymbolExpression can be used for both
// get the value of the symbol or modifying with the AssignmentExpression, which is
// quite inconsistent. The PointerAccessExpression can also be used for that purpose of
// get the value of something or changing it in case of the AssignmentExpression.
// To solve that problem I decided to create an special expression that would be used
// only to modify the value of something in memory.
