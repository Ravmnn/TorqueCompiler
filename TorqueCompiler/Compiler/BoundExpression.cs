using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




// class members of type BoundExpression must be settable because the TypeChecker may insert an implicit cast there




public abstract class BoundExpression(Expression syntax)
{
    public Expression Syntax { get; } = syntax;

    public virtual Type? Type { get; set; }




    public abstract void Process(IBoundExpressionProcessor processor);
    public abstract T Process<T>(IBoundExpressionProcessor<T> processor);


    public Token Source() => Syntax.Source();
    public SourceLocation Location() => Syntax.Location();
}




public class BoundLiteralExpression(LiteralExpression syntax) : BoundExpression(syntax)
{
    public new LiteralExpression Syntax => (base.Syntax as LiteralExpression)!;

    public ulong? Value { get; set; }




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessLiteral(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);
}




public class BoundBinaryExpression(BinaryExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public new BinaryExpression Syntax => (base.Syntax as BinaryExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;

    public override Type? Type => Left.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessBinary(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);
}




public class BoundUnaryExpression(UnaryExpression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public new UnaryExpression Syntax => (base.Syntax as UnaryExpression)!;

    public BoundExpression Expression { get; set; } = expression;

    public override Type? Type => Expression.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessUnary(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessUnary(this);
}




public class BoundGroupingExpression(GroupingExpression syntax, BoundExpression expression) : BoundExpression(syntax)
{
    public new GroupingExpression Syntax => (base.Syntax as GroupingExpression)!;

    public BoundExpression Expression { get; set; } = expression;

    public override Type? Type => Expression.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessGrouping(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);
}




public class BoundComparisonExpression(ComparisonExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public new ComparisonExpression Syntax => (base.Syntax as ComparisonExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;

    public override Type Type => PrimitiveType.Bool;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessComparison(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessComparison(this);
}




public class BoundEqualityExpression(EqualityExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public new EqualityExpression Syntax => (base.Syntax as EqualityExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;

    public override Type Type => PrimitiveType.Bool;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessEquality(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessEquality(this);
}




public class BoundLogicExpression(LogicExpression syntax, BoundExpression left, BoundExpression right) : BoundExpression(syntax)
{
    public new LogicExpression Syntax => (base.Syntax as LogicExpression)!;

    public BoundExpression Left { get; set; } = left;
    public BoundExpression Right { get; set; } = right;

    public override Type Type => PrimitiveType.Bool;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessLogic(this);

    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessLogic(this);
}




public class BoundSymbolExpression(SymbolExpression syntax, VariableSymbol symbol) : BoundExpression(syntax)
{
    public new SymbolExpression Syntax => (base.Syntax as SymbolExpression)!;

    public override Type Type => GetAddress ? new PointerType(Symbol.Type!) : Symbol.Type!;

    public VariableSymbol Symbol { get; } = symbol;
    public bool GetAddress => Syntax.GetAddress;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessSymbol(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);
}




public class BoundAssignmentExpression(AssignmentExpression syntax, BoundAssignmentReferenceExpression reference, BoundExpression value)
    : BoundExpression(syntax)
{
    public new AssignmentExpression Syntax => (base.Syntax as AssignmentExpression)!;

    public BoundAssignmentReferenceExpression Reference { get; set; } = reference;
    public BoundExpression Value { get; set; } = value;

    public override Type? Type => Reference.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAssignment(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);
}



// An assignment reference is a memory address (pointer) that will only be used to access the location
// in memory of something to modify its value and cannot be used to get the value inside that location (write-only).
// I was trying to find a way to make the AssignmentExpression work for anything that had
// a valid memory address and could be modified. The SymbolExpression can be used for both
// get the value of the symbol or modifying with the AssignmentExpression, which is
// quite inconsistent. The PointerAccessExpression can also be used for that purpose of
// get the value of something or changing it in case of the AssignmentExpression.
// To solve that problem I decided to create an special expression that would be used
// only to modify the value of something in memory.

public class BoundAssignmentReferenceExpression(Expression syntax, BoundExpression reference) : BoundExpression(syntax)
{
    public BoundExpression Reference { get; set; } = reference;

    public override Type? Type => Reference.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessAssignmentReference(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessAssignmentReference(this);
}




public class BoundPointerAccessExpression(PointerAccessExpression syntax, BoundExpression pointer) : BoundExpression(syntax)
{
    public new PointerAccessExpression Syntax => (base.Syntax as PointerAccessExpression)!;

    public BoundExpression Pointer { get; set; } = pointer;

    public override Type Type => (Pointer.Type as PointerType)!.Type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessPointerAccess(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessPointerAccess(this);
}




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




public class BoundCastExpression(CastExpression syntax, BoundExpression value) : BoundExpression(syntax)
{
    public new CastExpression Syntax => (base.Syntax as CastExpression)!;

    public BoundExpression Value { get; set; } = value;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessCast(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessCast(this);
}




public class BoundImplicitCastExpression(BoundExpression value, Type type) : BoundExpression(value.Syntax)
{
    public BoundExpression Value { get; } = value;
    public override Type? Type { get; set; } = type;




    public override void Process(IBoundExpressionProcessor processor)
        => processor.ProcessImplicitCast(this);


    public override T Process<T>(IBoundExpressionProcessor<T> processor)
        => processor.ProcessImplicitCast(this);
}
