using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class TorqueCompiler : IBoundStatementProcessor, IBoundExpressionProcessor<LLVMValueRef>
{
    private const string FunctionEntryBlockName = "entry";


    // if debug metadata generation is desired, this property must be set, since
    // debug metadata uses some file information
    public FileInfo? File { get; }


    private readonly LLVMModuleRef _module = LLVMModuleRef.CreateWithName("MainModule");
    public LLVMModuleRef Module => _module;

    public LLVMBuilderRef Builder { get; } = LLVMBuilderRef.Create(LLVMContextRef.Global);

    public LLVMTargetMachineRef TargetMachine { get; private set; }
    public LLVMTargetDataRef TargetData { get; private set; }

    public DebugMetadataGenerator? Debug { get; }


    // TODO: move these to DebugMetadataGenerator, since only it uses them
    public Scope GlobalScope { get; }
    public Scope Scope { get; private set; }


    public BoundStatement[] Statements { get; }




    public TorqueCompiler(IEnumerable<BoundStatement> statements, Scope globalGlobalScope, FileInfo? file = null, bool generateDebugMetadata = false)
    {
        // TODO: add optimization command line options (later... this is more useful after this language is able to do more stuff)

        // TODO: add floats
        // TODO: add function calling

        // TODO: only pointers type (T*) should be able to modify the memory itself:
        // normal types that acquires the memory of something (&value) should treat the address returned as a normal integer

        // TODO: add pointers

        // TODO: weird error when there's still code after a return statement

        // TODO: make this user's choice (command line options)
        const string Triple = "x86_64-pc-linux-gnu";

        InitializeTargetMachine(Triple);
        SetupModuleTargetProperties(Triple);

        GlobalScope = globalGlobalScope;
        Scope = GlobalScope;

        File = file;

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
        // Many parts of the code in this file assumes that there are values in some nullables
        // as "BoundExpression.Type", and that "BoundExpression.Syntax" (or "BoundStatement.Syntax")
        // stores the correct value according to the real expression (or statement).
        // Because of that, no null check is performed and if any system exception is thrown
        // due to null access or bad type conversion, or it is a bug, or the caller of the compiler API
        // didn't set up (binding, type checking...) the statements correctly.


        foreach (var statement in Statements)
            Process(statement);

        Debug?.FinalizeGenerator();

        return Module.PrintToString();
    }




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




    private void DebugGenerateScope(Scope scope, TokenLocation location, LLVMMetadataRef? debugFunctionReference = null)
    {
        var llvmDebugScopeMetadata = DebugCreateLexicalScope(location);
        llvmDebugScopeMetadata = debugFunctionReference ?? llvmDebugScopeMetadata;

        scope.DebugMetadata = llvmDebugScopeMetadata;
    }




    public void Process(BoundStatement statement)
    {
        statement.Process(this);
    }




    public void ProcessExpression(BoundExpressionStatement statement)
    {
        DebugSetLocationTo(statement.Source());
        Process(statement.Expression);
    }




    public void ProcessDeclaration(BoundDeclarationStatement statement)
    {
        var symbol = statement.Symbol;

        var name = symbol.Name;
        var type = symbol.Type!.Value;
        var llvmType = type.PrimitiveToLLVMType();

        var statementSource = statement.Source();


        DebugSetLocationTo(statementSource);

        var reference = Builder.BuildAlloca(llvmType, name);
        var debugReference = DebugGenerateLocalVariable(name, type, statementSource, reference);

        symbol.LLVMReference = reference;
        symbol.LLVMType = llvmType;
        symbol.LLVMDebugMetadata = debugReference;

        Builder.BuildStore(Process(statement.Value), reference);
        DebugUpdateLocalVariableValue(name, statementSource);

        DebugSetLocationTo(null);
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
    {
        var syntax = (statement.Syntax as FunctionDeclarationStatement)!;
        var symbol = statement.Symbol;

        var functionName = symbol.Name;
        var functionReturnType = symbol.ReturnType!.Value;
        var functionLocation = syntax.Source();
        var parameterTypes = statement.Symbol.Parameters!;

        var llvmParameterTypes = parameterTypes.Select(parameter => parameter.PrimitiveToLLVMType()).ToArray();
        var llvmFunctionType = LLVMTypeRef.CreateFunction(functionReturnType.PrimitiveToLLVMType(), llvmParameterTypes.ToArray());
        var llvmFunctionReference = Module.AddFunction(functionName, llvmFunctionType);
        var llvmFunctionDebugMetadata = DebugGenerateFunction(llvmFunctionReference, functionName, functionLocation, functionReturnType, parameterTypes);

        symbol.LLVMReference = llvmFunctionReference;
        symbol.LLVMType = llvmFunctionType;

        var entry = llvmFunctionReference.AppendBasicBlock(FunctionEntryBlockName);
        Builder.PositionAtEnd(entry);

        ProcessScopeBlock(statement.Body, llvmFunctionDebugMetadata);
    }




    public void ProcessReturn(BoundReturnStatement statement)
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




    public void ProcessBlock(BoundBlockStatement statement)
        => ProcessScopeBlock(statement);


    private void ProcessScopeBlock(BoundBlockStatement statement, LLVMMetadataRef? function = null)
    {
        var oldScope = Scope;
        Scope = statement.Scope;

        DebugGenerateScope(statement.Scope, statement.Source(), function);

        foreach (var subStatement in statement.Statements)
            Process(subStatement);

        Scope = oldScope;
    }








    public LLVMValueRef Process(BoundExpression expression)
        => expression.Process(this);




    public LLVMValueRef ProcessLiteral(BoundLiteralExpression expression)
    {
        var llvmType = expression.Type!.Value.PrimitiveToLLVMType();
        var value = expression.Value!.Value;

        var llvmReference = LLVMValueRef.CreateConstInt(llvmType, value);

        return llvmReference;
    }




    public LLVMValueRef ProcessBinary(BoundBinaryExpression expression)
    {
        var syntax = (expression.Syntax as BinaryExpression)!;

        var right = Process(expression.Left);
        var left = Process(expression.Right);

        return syntax.Operator.Type switch
        {
            TokenType.Plus => Builder.BuildAdd(left, right, "sum"),
            TokenType.Minus => Builder.BuildSub(left, right, "sub"),
            TokenType.Star => Builder.BuildMul(left, right, "mult"),
            TokenType.Slash => Builder.BuildSDiv(left, right, "div"),

            _ => throw new UnreachableException()
        };
    }




    public LLVMValueRef ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);


    public LLVMValueRef ProcessSymbol(BoundSymbolExpression expression)
    {
        var symbol = expression.Symbol;

        var llvmReference = symbol.LLVMReference!.Value;
        var llvmType = symbol.LLVMType!.Value;

        if (expression.GetAddress)
            return llvmReference;

        return Builder.BuildLoad2(llvmType, llvmReference, "value");
    }




    public LLVMValueRef ProcessAssignment(BoundAssignmentExpression expression)
    {
        // processing "expression.Symbol" (a BoundExpression) is not actually needed, since
        // it will always be a "BoundSymbolExpression", so it is possible to get the symbol information
        // directly.

        var symbol = expression.Symbol.Symbol;
        var value = Process(expression.Value);

        var pointer = symbol.LLVMReference!.Value;

        var result = Builder.BuildStore(value, pointer);
        DebugUpdateLocalVariableValue(symbol.Name, expression.Source());

        return result;
    }




    public LLVMValueRef ProcessCall(BoundCallExpression expression)
    {
        var function = Process(expression.Callee);
        var arguments = expression.Arguments.Select(Process).ToArray();

        return Builder.BuildCall2(function.TypeOf.ElementType, function, arguments, "retval");
    }




    public LLVMValueRef ProcessCast(BoundCastExpression expression)
    {
        var value = Process(expression.Value);
        var toType = expression.Type!.Value.PrimitiveToLLVMType();

        var sourceTypeSize = SizeOf(value.TypeOf);
        var targetTypeSize = SizeOf(toType);

        if (sourceTypeSize < targetTypeSize)
            return Builder.BuildIntCast(value, toType, "incrcast");

        if (sourceTypeSize > targetTypeSize)
            return Builder.BuildTrunc(value, toType, "decrcast");

        return value;
    }
}
