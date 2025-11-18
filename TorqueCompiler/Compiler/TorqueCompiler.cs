using System.Collections.Generic;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class TorqueCompiler : IStatementProcessor, IExpressionProcessor
{
    public const string DefaultEntryPoint = "main";

    private const string FunctionEntryBlockName = "entry";




    private LLVMModuleRef _module = LLVMModuleRef.CreateWithName("MainModule");
    private LLVMBuilderRef _builder = LLVMBuilderRef.Create(LLVMContextRef.Global);

    private Stack<LLVMValueRef> _valueStack = new Stack<LLVMValueRef>();




    public Statement[] Statements { get; }




    public TorqueCompiler(IEnumerable<Statement> statements)
    {
        Statements = statements.ToArray();
    }




    public void PushValue(LLVMValueRef value)
        => _valueStack.Push(value);


    public LLVMValueRef PopValue()
        => _valueStack.Pop();




    public string Compile()
    {
        foreach (var statement in Statements)
            Process(statement);

        return _module.PrintToString();
    }




    public void Process(Expression expression)
        => expression.Process(this);


    public void Process(Statement statement)
        => statement.Process(this);




    public void ProcessExpression(ExpressionStatement statement)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessDeclaration(DeclarationStatement statement)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        var paramTypes
            = from parameter in statement.Parameters select parameter.Type.TokenToLLVMType();
        var functionType = LLVMTypeRef.CreateFunction(statement.ReturnType.TokenToLLVMType(), paramTypes.ToArray());
        var function = _module.AddFunction(statement.Name.Lexeme, functionType);

        var entry = function.AppendBasicBlock(FunctionEntryBlockName);
        _builder.PositionAtEnd(entry);

        foreach (var subStatement in statement.Body.Statements)
            Process(subStatement);
    }


    public void ProcessReturn(ReturnStatement statement)
    {
        if (statement.Expression is not null)
        {
            Process(statement.Expression);
            _builder.BuildRet(PopValue());
        }
        else
            _builder.BuildRetVoid();
    }


    public void ProcessBlock(BlockStatement statement)
    {
        throw new System.NotImplementedException();
    }




    public void ProcessLiteral(LiteralExpression expression)
    {
        var value = LLVMValueRef.CreateConstInt(expression.Type.PrimitiveToLLVMType(), ulong.Parse(expression.Value.Lexeme));
        PushValue(value);
    }


    public void ProcessBinary(BinaryExpression expression)
    {
        Process(expression.Left);
        Process(expression.Right);

        var right = PopValue();
        var left = PopValue();

        switch (expression.Operator.Type)
        {
            case TokenType.Plus:
                PushValue(_builder.BuildAdd(left, right, "sum"));
                break;

            case TokenType.Minus:
                PushValue(_builder.BuildSub(left, right, "sub"));
                break;

            case TokenType.Star:
                PushValue(_builder.BuildMul(left, right, "mult"));
                break;

            case TokenType.Slash:
                PushValue(_builder.BuildSDiv(left, right, "div"));
                break;
        }
    }


    public void ProcessGrouping(GroupingExpression expression)
    {
        Process(expression.Expression);
    }


    public void ProcessIdentifier(IdentifierExpression expression)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessCall(CallExpression expression)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessCast(CastExpression expression)
    {
        throw new System.NotImplementedException();
    }
}
