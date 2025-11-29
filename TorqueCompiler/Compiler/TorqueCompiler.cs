using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class TorqueCompiler : IStatementProcessor, IExpressionProcessor
{
    private const string FunctionEntryBlockName = "entry";




    private readonly Stack<LLVMValueRef> _valueStack = new Stack<LLVMValueRef>();


    private readonly LLVMModuleRef _module = LLVMModuleRef.CreateWithName("MainModule");
    public LLVMModuleRef Module => _module;

    public LLVMBuilderRef Builder { get; } = LLVMBuilderRef.Create(LLVMContextRef.Global);

    public LLVMTargetMachineRef TargetMachine { get; private set; }
    public LLVMTargetDataRef TargetData { get; private set; }

    public DebugMetadataGenerator? Debug { get; }


    public Scope GlobalScope { get; } = [];
    public Scope Scope { get; private set; }


    public Statement[] Statements { get; }




    public TorqueCompiler(IEnumerable<Statement> statements, bool generateDebugMetadata = false)
    {
        // TODO: add optimization command line options (later... this is more useful after this language is able to do more stuff)

        // TODO: add floats

        // TODO: only pointers type (T*) should be able to modify the memory itself:
        // normal types that acquires the memory of something (&value) should treat the address returned as a normal integer

        // TODO: make the Parser handle things like identifier checking, type checking, etc:
        // - add type checking
        // - identifier/scope checking
        // The compiler should only compile things and not be responsible for that

        // TODO: make this user's choice (command line options)
        const string Triple = "x86_64-pc-linux-gnu";

        InitializeTargetMachine(Triple);
        SetupModuleTargetProperties(Triple);

        Scope = GlobalScope;

        if (generateDebugMetadata)
            Debug = new DebugMetadataGenerator(this);


        Statements = statements.ToArray();
    }


    private void InitializeTargetMachine(string triple)
    {
        LLVM.InitializeNativeTarget();
        LLVM.InitializeNativeAsmPrinter();

        if (!LLVMTargetRef.TryGetTargetFromTriple(triple, out var target, out _))
            throw new InvalidOperationException("LLVM doesn't support this target.");

        TargetMachine = target.CreateTargetMachine(
            triple, "generic", "",
            LLVMCodeGenOptLevel.LLVMCodeGenLevelDefault, LLVMRelocMode.LLVMRelocPIC,
            LLVMCodeModel.LLVMCodeModelDefault
        );
    }


    private unsafe void SetupModuleTargetProperties(string triple)
    {
        TargetData = TargetMachine.CreateTargetDataLayout();

        _module.Target = triple;

        var ptr = LLVM.CopyStringRepOfTargetData(TargetData);
        _module.DataLayout = Marshal.PtrToStringAnsi((IntPtr)ptr) ?? throw new InvalidOperationException("Couldn't create data layout.");
    }




    public string Compile()
    {
        foreach (var statement in Statements)
            Process(statement);

        Debug?.FinalizeGenerator();

        return Module.PrintToString();
    }




    private void PushValue(LLVMValueRef value)
        => _valueStack.Push(value);


    private LLVMValueRef PopValue()
    {
        if (_valueStack.Count == 0)
            throw new InvalidOperationException("Value stack is empty.");

        return _valueStack.Pop();
    }


    private LLVMValueRef Consume(Expression expression)
    {
        Process(expression);
        return PopValue();
    }




    private void ScopeEnter(TokenLocation location, LLVMMetadataRef? debugFunctionReference = null)
    {
        var debugScope = DebugCreateLexicalScope(location);
        debugScope = debugFunctionReference ?? debugScope;

        Scope = new Scope(Scope, debugScope);
    }


    private void ScopeExit()
        => Scope = Scope.Parent ?? throw new InvalidOperationException("Cannot exit the global scope.");




    private int SizeOf(LLVMTypeRef type)
        => type.SizeOfThis(TargetData);




    private LLVMMetadataRef? DebugSetLocationTo(TokenLocation? location)
    {
        if (location is null)
        {
            Debug?.SetLocation();
            return null;
        }

        return Debug?.SetLocation(location.Value.Line, location.Value.Start);
    }


    private LLVMMetadataRef? DebugCreateLocation(TokenLocation location)
        => Debug?.CreateDebugLocation(location.Line, location.Start);


    private LLVMMetadataRef? DebugCreateLexicalScope(TokenLocation location)
        => Debug?.CreateLexicalScope(location.Line, location.Start);




    private LLVMMetadataRef? DebugGenerateFunction(LLVMValueRef function, string functionName, TokenLocation functionLocation, PrimitiveType functionReturnType, IEnumerable<PrimitiveType> parameterTypes)
        => Debug?.GenerateFunction(function, functionName, functionLocation.Line, functionReturnType, parameterTypes.ToArray());




    private LLVMMetadataRef? DebugGenerateLocalVariable(string name, PrimitiveType type, Token statementSource, LLVMValueRef alloca)
    {
        var location = statementSource.Location;
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);

        if (llvmLocation is not null)
            return Debug?.GenerateLocalVariable(name, type, location.Line, alloca, llvmLocation.Value);

        return null;
    }


    private LLVMDbgRecordRef? DebugUpdateLocalVariableValue(string name, TokenLocation location)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);

        return Debug?.UpdateLocalVariableValue(name, llvmLocation!.Value);
    }




    public void Process(Expression expression)
        => expression.Process(this);


    public void Process(Statement statement)
    {
        statement.Process(this);

        _valueStack.Clear();
    }




    public void ProcessExpression(ExpressionStatement statement)
    {
        DebugSetLocationTo(statement.Source());
        Process(statement.Expression);
    }




    public void ProcessDeclaration(DeclarationStatement statement)
    {
        var name = statement.Name.Lexeme;
        var type = statement.Type.TokenToPrimitive();
        var llvmType = type.PrimitiveToLLVMType();

        var statementSource = statement.Source();


        DebugSetLocationTo(statementSource);

        var reference = Builder.BuildAlloca(llvmType, name);
        var debugReference = DebugGenerateLocalVariable(name, type, statementSource, reference);

        Scope.Add(new Identifier(reference, llvmType, debugReference));

        Builder.BuildStore(Consume(statement.Value), reference);
        DebugUpdateLocalVariableValue(name, statementSource);

        DebugSetLocationTo(null);
    }




    public void ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        var parameterTypes
            = from parameter in statement.Parameters select parameter.Type.TokenToPrimitive();

        var llvmParameterTypes
            = from parameter in statement.Parameters select parameter.Type.TokenToLLVMType();

        var functionName = statement.Name.Lexeme;
        var functionReturnType = statement.ReturnType.TokenToPrimitive();
        var functionType = LLVMTypeRef.CreateFunction(functionReturnType.PrimitiveToLLVMType(), llvmParameterTypes.ToArray());
        var functionLocation = statement.Name.Location;

        var function = Module.AddFunction(functionName, functionType);
        var functionDebugReference = DebugGenerateFunction(function, functionName, functionLocation, functionReturnType, parameterTypes);

        Scope.Add(new Identifier(function, functionType, functionDebugReference));

        var entry = function.AppendBasicBlock(FunctionEntryBlockName);
        Builder.PositionAtEnd(entry);

        ProcessScopeBlock(statement.Body, functionDebugReference);
    }




    public void ProcessReturn(ReturnStatement statement)
    {
        if (statement.Expression is not null)
        {
            DebugSetLocationTo(statement.Source());
            Builder.BuildRet(Consume(statement.Expression));
            DebugSetLocationTo(null);
        }
        else
            Builder.BuildRetVoid();
    }




    public void ProcessBlock(BlockStatement statement)
        => ProcessScopeBlock(statement);


    private void ProcessScopeBlock(BlockStatement statement, LLVMMetadataRef? function = null)
    {
        ScopeEnter(statement.Source(), function);

        foreach (var subStatement in statement.Statements)
            Process(subStatement);

        ScopeExit();
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
                PushValue(Builder.BuildAdd(left, right, "sum"));
                break;

            case TokenType.Minus:
                PushValue(Builder.BuildSub(left, right, "sub"));
                break;

            case TokenType.Star:
                PushValue(Builder.BuildMul(left, right, "mult"));
                break;

            case TokenType.Slash:
                PushValue(Builder.BuildSDiv(left, right, "div"));
                break;
        }
    }




    public void ProcessGrouping(GroupingExpression expression)
    {
        Process(expression.Expression);
    }




    public void ProcessIdentifier(IdentifierExpression expression)
    {
        var identifier = Scope.GetIdentifier(expression.Identifier.Lexeme);
        var value = expression.GetAddress ? identifier.Address : Builder.BuildLoad2(identifier.Type, identifier.Address, "value");

        PushValue(value);
    }




    public void ProcessAssignment(AssignmentExpression expression)
    {
        var identifier = Consume(expression.Identifier);
        var value = Consume(expression.Value);

        PushValue(Builder.BuildStore(value, identifier));

        var identifierName = expression.Identifier.Identifier.Lexeme;
        DebugUpdateLocalVariableValue(identifierName, expression.Source());
    }




    public void ProcessCall(CallExpression expression)
    {
        var function = Consume(expression.Callee);

        var arguments
            = from argument in expression.Arguments select Consume(argument);

        Builder.BuildCall2(function.TypeOf.ElementType, function, arguments.ToArray(), "retval");
    }




    public void ProcessCast(CastExpression expression)
    {
        var value = Consume(expression.Expression);
        var toType = expression.Type.TokenToLLVMType();

        var sourceTypeSize = SizeOf(value.TypeOf);
        var targetTypeSize = SizeOf(toType);

        if (sourceTypeSize < targetTypeSize)
            PushValue(Builder.BuildIntCast(value, toType, "incrcast"));

        else if (sourceTypeSize > targetTypeSize)
            PushValue(Builder.BuildTrunc(value, toType, "decrcast"));

        else
            PushValue(value);
    }
}
