using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class TorqueCompiler : IStatementProcessor, IExpressionProcessor<LLVMValueRef>
{
    private const string FunctionEntryBlockName = "entry";


    public FileInfo? FileInfo { get; init; }


    private readonly LLVMModuleRef _module = LLVMModuleRef.CreateWithName("MainModule");
    public LLVMModuleRef Module => _module;

    public LLVMBuilderRef Builder { get; } = LLVMBuilderRef.Create(LLVMContextRef.Global);

    public LLVMTargetMachineRef TargetMachine { get; private set; }
    public LLVMTargetDataRef TargetData { get; private set; }

    public DebugMetadataGenerator? Debug { get; }


    public Scope Scope { get; }


    public Statement[] Statements { get; }




    public TorqueCompiler(IEnumerable<Statement> statements, bool generateDebugMetadata = false)
    {
        // TODO: add optimization command line options (later... this is more useful after this language is able to do more stuff)

        // TODO: add floats
        // TODO: add function calling

        // TODO: only pointers type (T*) should be able to modify the memory itself:
        // normal types that acquires the memory of something (&value) should treat the address returned as a normal integer

        // TODO: create semantic analysis:
        // - identifier resolver
        // - type checker

        // TODO: make this user's choice (command line options)
        const string Triple = "x86_64-pc-linux-gnu";

        InitializeTargetMachine(Triple);
        SetupModuleTargetProperties(Triple);

        //Scope = GlobalScope;

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




    private void ScopeEnter(TokenLocation location, LLVMMetadataRef? debugFunctionReference = null)
    {
        var debugScope = DebugCreateLexicalScope(location);
        debugScope = debugFunctionReference ?? debugScope;

        // Scope = new Scope(Scope) { DebugMetadata = debugScope };
    }


    private void ScopeExit()
    {}// => Scope = Scope.Parent ?? throw new InvalidOperationException("Cannot exit the global scope.");




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




    public void Process(Statement statement)
    {
        statement.Process(this);
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

        // Scope.Symbols.Add(new CompilerIdentifier(reference, llvmType, debugReference));

        Builder.BuildStore(Process(statement.Value), reference);
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

        // Scope.Add(new CompilerIdentifier(function, functionType, functionDebugReference));

        var entry = function.AppendBasicBlock(FunctionEntryBlockName);
        Builder.PositionAtEnd(entry);

        ProcessScopeBlock(statement.Body, functionDebugReference);
    }




    public void ProcessReturn(ReturnStatement statement)
    {
        if (statement.Expression is not null)
        {
            DebugSetLocationTo(statement.Source());
            Builder.BuildRet(Process(statement.Expression));
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








    public LLVMValueRef Process(Expression expression)
        => expression.Process(this);




    public LLVMValueRef ProcessLiteral(LiteralExpression expression)
    {
        // var value = LLVMValueRef.CreateConstInt(expression.Type.PrimitiveToLLVMType(), ulong.Parse(expression.Value.Lexeme));
        // PushValue(value);

        throw new NotImplementedException();
    }




    public LLVMValueRef ProcessBinary(BinaryExpression expression)
    {
        var right = Process(expression.Left);
        var left = Process(expression.Right);

        switch (expression.Operator.Type)
        {
            case TokenType.Plus:
                return Builder.BuildAdd(left, right, "sum");

            case TokenType.Minus:
                return Builder.BuildSub(left, right, "sub");

            case TokenType.Star:
                return Builder.BuildMul(left, right, "mult");

            case TokenType.Slash:
                return Builder.BuildSDiv(left, right, "div");
        }

        throw new UnreachableException();
    }




    public LLVMValueRef ProcessGrouping(GroupingExpression expression)
        => Process(expression.Expression);


    public LLVMValueRef ProcessIdentifier(IdentifierExpression expression)
    {
        // var identifier = Scope.GetIdentifier(expression.Identifier.Lexeme);
        // var value = expression.GetAddress ? identifier.Address : Builder.BuildLoad2(identifier.Type, identifier.Address, "value");
        //
        // PushValue(value);

        throw new NotImplementedException();
    }




    public LLVMValueRef ProcessAssignment(AssignmentExpression expression)
    {
        var identifier = Process(expression.Identifier);
        var value = Process(expression.Value);

        var result = Builder.BuildStore(value, identifier);

        var identifierName = expression.Identifier.Identifier.Lexeme;
        DebugUpdateLocalVariableValue(identifierName, expression.Source());

        return result;
    }




    public LLVMValueRef ProcessCall(CallExpression expression)
    {
        var function = Process(expression.Callee);
        var arguments = expression.Arguments.Select(Process).ToArray();

        return Builder.BuildCall2(function.TypeOf.ElementType, function, arguments, "retval");
    }




    public LLVMValueRef ProcessCast(CastExpression expression)
    {
        var value = Process(expression.Expression);
        var toType = expression.Type.TokenToLLVMType();

        var sourceTypeSize = SizeOf(value.TypeOf);
        var targetTypeSize = SizeOf(toType);

        if (sourceTypeSize < targetTypeSize)
            return Builder.BuildIntCast(value, toType, "incrcast");

        if (sourceTypeSize > targetTypeSize)
            return Builder.BuildTrunc(value, toType, "decrcast");

        return value;
    }
}
