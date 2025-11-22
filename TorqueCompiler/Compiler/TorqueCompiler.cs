using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using LLVMSharp.Interop;


namespace Torque.Compiler;




public class TorqueCompiler : IStatementProcessor, IExpressionProcessor
{
    private const string FunctionEntryBlockName = "entry";




    private LLVMModuleRef _module = LLVMModuleRef.CreateWithName("MainModule");
    private LLVMBuilderRef _builder = LLVMBuilderRef.Create(LLVMContextRef.Global);

    private LLVMTargetMachineRef _targetMachine;
    private LLVMTargetDataRef _targetData;

    private readonly Stack<LLVMValueRef> _valueStack = new Stack<LLVMValueRef>();


    private readonly Scope _globalScope = [];
    private Scope _scope;




    public Statement[] Statements { get; }




    public TorqueCompiler(IEnumerable<Statement> statements)
    {
        // TODO: add optimization command line options
        // TODO: add debugging

        // TODO: make this user's choice (command line options)
        const string Triple = "x86_64-pc-linux-gnu";

        InitializeTargetMachine(Triple);
        SetupModuleTargetProperties(Triple);

        _scope = _globalScope;


        Statements = statements.ToArray();
    }


    private void InitializeTargetMachine(string triple)
    {
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();

        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out _))
            throw new InvalidOperationException("LLVM doesn't support this target.");

        _targetMachine = target.CreateTargetMachine(
            triple, "generic", "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocPIC,
            LLVMCodeModel.LLVMCodeModelDefault
        );
    }


    private void SetupModuleTargetProperties(string triple)
    {
        _targetData = _targetMachine.CreateTargetDataLayout();

        _module.Target = triple;

        unsafe
        {
            var ptr = LLVM.CopyStringRepOfTargetData(_targetData);
            _module.DataLayout = Marshal.PtrToStringAnsi((IntPtr)ptr) ?? throw new InvalidOperationException("Couldn't create data layout.");
        }
    }


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
        Process(statement.Expression);
    }


    public void ProcessDeclaration(DeclarationStatement statement)
    {
        var name = statement.Name.Lexeme;
        var type = statement.Type.TokenToLLVMType();

        var identifier = _builder.BuildAlloca(type, name);
        var value = Consume(statement.Value);

        _builder.BuildStore(value, identifier);

        _scope.Add(new Identifier(identifier, type));
    }


    public void ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        var parameterTypes
            = from parameter in statement.Parameters select parameter.Type.TokenToLLVMType();

        var functionName = statement.Name.Lexeme;
        var functionType = LLVMTypeRef.CreateFunction(statement.ReturnType.TokenToLLVMType(), parameterTypes.ToArray());
        var function = _module.AddFunction(functionName, functionType);

        _scope.Add(new Identifier(function, functionType));

        var entry = function.AppendBasicBlock(FunctionEntryBlockName);
        _builder.PositionAtEnd(entry);

        ProcessBlock(statement.Body);
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
        var oldScope = _scope;
        _scope = new Scope(_scope);

        foreach (var subStatement in statement.Statements)
            Process(subStatement);

        _scope = oldScope;
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
        var identifier = _scope.GetIdentifier(expression.Identifier.Lexeme);
        var value = _builder.BuildLoad2(identifier.Type, identifier.Reference, "value");

        PushValue(value);
    }


    public void ProcessCall(CallExpression expression)
    {
        var function = Consume(expression.Callee);

        var arguments
            = from argument in expression.Arguments select Consume(argument);

        _builder.BuildCall2(function.TypeOf.ElementType, function, arguments.ToArray(), "retval");
    }


    public void ProcessCast(CastExpression expression)
    {
        var value = Consume(expression.Expression);
        var toType = expression.Type.TokenToLLVMType();

        var sourceTypeSize = SizeOf(value.TypeOf);
        var targetTypeSize = SizeOf(toType);

        if (sourceTypeSize < targetTypeSize)
            PushValue(_builder.BuildIntCast(value, toType, "incrcast"));

        else if (sourceTypeSize > targetTypeSize)
            PushValue(_builder.BuildTrunc(value, toType, "decrcast"));

        else
            PushValue(value);
    }




    private void PushValue(LLVMValueRef value)
        => _valueStack.Push(value);


    private LLVMValueRef PopValue()
        => _valueStack.Pop();


    private LLVMValueRef Consume(Expression expression)
    {
        Process(expression);
        return PopValue();
    }




    private int SizeOf(LLVMTypeRef type)
        => type.SizeOfThis(_targetData);
}
