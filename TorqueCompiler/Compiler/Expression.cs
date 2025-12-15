using System.Collections.Generic;


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
}

// TODO: expression creation is too boilerplate:
// create base classes for common patterns, like binary expresssions
// do that for the parser too... common methods to handle binary expressions




public abstract class Expression
{
    public abstract void Process(IExpressionProcessor processor);
    public abstract T Process<T>(IExpressionProcessor<T> processor);


    public abstract Token Source();
}




public class LiteralExpression(Token value) : Expression
{
    public Token Value { get; } = value;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLiteral(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLiteral(this);


    public override Token Source() => Value;
}




public class BinaryExpression(Expression left, Token @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Token Operator { get; } = @operator;
    public Expression Right { get; } = right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessBinary(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessBinary(this);


    public override Token Source() => Operator;
}




public class UnaryExpression(Token @operator, Expression expression) : Expression
{
    public Token Operator { get; } = @operator;
    public Expression Expression { get; } = expression;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessUnary(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessUnary(this);


    public override Token Source() => Operator;
}




public class GroupingExpression(Expression expression) : Expression
{
    public Expression Expression { get; } = expression;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessGrouping(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessGrouping(this);


    public override Token Source() => Expression.Source();
}




public class ComparisonExpression(Expression left, Token @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Token Operator { get; } = @operator;
    public Expression Right { get; } = right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessComparison(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessComparison(this);


    public override Token Source()
        => Operator;
}




public class EqualityExpression(Expression left, Token @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Token Operator { get; } = @operator;
    public Expression Right { get; } = right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessEquality(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessEquality(this);


    public override Token Source()
        => Operator;
}




public class LogicExpression(Expression left, Token @operator, Expression right) : Expression
{
    public Expression Left { get; } = left;
    public Token Operator { get; } = @operator;
    public Expression Right { get; } = right;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessLogic(this);

    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessLogic(this);


    public override Token Source()
        => Operator;
}




public class SymbolExpression(Token identifier, bool getAddress = false) : Expression
{
    public Token Identifier { get; } = identifier;
    public bool GetAddress { get; } = getAddress;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessSymbol(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessSymbol(this);


    public override Token Source() => Identifier;
}




public class AssignmentExpression(Expression pointer, Token @operator, Expression value) : Expression
{
    public Expression Pointer { get; } = pointer;
    public Token Operator { get; } = @operator;
    public Expression Value { get; } = value;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessAssignment(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessAssignment(this);


    public override Token Source() => Operator;
}




public class PointerAccessExpression(Token @operator, Expression pointer) : Expression
{
    public Token Operator { get; } = @operator;
    public Expression Pointer { get; } = pointer;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessPointerAccess(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessPointerAccess(this);


    public override Token Source() => Operator;
}




public class CallExpression(Token leftParen, Expression callee, IReadOnlyList<Expression> arguments) : Expression
{
    public Token LeftParen { get; } = leftParen;
    public Expression Callee { get; } = callee;
    public IReadOnlyList<Expression> Arguments { get; } = arguments;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCall(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCall(this);


    public override Token Source() => LeftParen;
}





public class CastExpression(Expression expression, Token keyword, TypeName type) : Expression
{
    public Expression Expression { get; } = expression;
    public Token Keyword { get; } = keyword;
    public TypeName Type { get; } = type;




    public override void Process(IExpressionProcessor processor)
        => processor.ProcessCast(this);


    public override T Process<T>(IExpressionProcessor<T> processor)
        => processor.ProcessCast(this);


    public override Token Source() => Keyword;
}
