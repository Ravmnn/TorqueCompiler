// ReSharper disable LocalizableElement


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


    public Scope GlobalScope { get; }
    public Scope Scope { get; private set; }

    public BoundStatement[] Statements { get; }




    public TorqueCompiler(IEnumerable<BoundStatement> statements, Scope globalScope, FileInfo? file = null, bool generateDebugMetadata = false)
    {
        // TODO: add optimization command line options (later... this is more useful after this language is able to do more stuff)
        // TODO: add importing system

        // TODO: add floats
        // TODO: add function calling
        // TODO: in order to call functions, you'll probably need to implement a way to represent function type:
        // int(int16, int16, bool)...

        // TODO: add boolean expressions

        // TODO: make infinite indirection pointers? (T****...) or limit to double? (T**)

        // TODO: make this user's choice (command line options)
        const string Triple = "x86_64-pc-linux-gnu";

        InitializeTargetMachine(Triple);
        SetupModuleTargetProperties(Triple);

        File = file;


        GlobalScope = globalScope;
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




    private static void UnreachableCode()
        => throw new UnreachableCodeControl();




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




    private LLVMMetadataRef? DebugGenerateFunction(LLVMValueRef function, string functionName, TokenLocation functionLocation, FunctionType type)
        => Debug?.GenerateFunction(function, functionName, functionLocation.Line, type);




    private LLVMMetadataRef? DebugGenerateLocalVariable(string name, Type type, Token statementSource, LLVMValueRef alloca)
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


    private LLVMDbgRecordRef? DebugUpdateLocalVariableValue(LLVMValueRef reference, TokenLocation location)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);

        return Debug?.UpdateLocalVariableValue(reference, llvmLocation!.Value);
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
        DebugSetLocationTo(null);
    }




    public void ProcessDeclaration(BoundDeclarationStatement statement)
    {
        var symbol = statement.Symbol;

        var name = symbol.Name;
        var type = symbol.Type!;
        var llvmType = type.TypeToLLVMType();

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
        var functionReturnType = symbol.Type!.ReturnType;
        var functionLocation = syntax.Source();
        var parameters = statement.Symbol.Parameters;
        var parameterTypes = symbol.Type.ParametersType;

        var llvmParameterTypes = (from parameter in parameterTypes select parameter.TypeToLLVMType()).ToArray();
        var llvmFunctionType = LLVMTypeRef.CreateFunction(functionReturnType.TypeToLLVMType(), llvmParameterTypes);
        var llvmFunctionReference = Module.AddFunction(functionName, llvmFunctionType);
        var llvmFunctionDebugMetadata = DebugGenerateFunction(llvmFunctionReference, functionName, functionLocation, symbol.Type);

        symbol.LLVMReference = llvmFunctionReference;
        symbol.LLVMType = llvmFunctionType;
        symbol.LLVMDebugMetadata = llvmFunctionDebugMetadata;

        var entry = llvmFunctionReference.AppendBasicBlock(FunctionEntryBlockName);
        Builder.PositionAtEnd(entry);

        ProcessFunctionParametersDeclaration(llvmFunctionReference, parameters);

        ProcessScopeBlock(statement.Body, llvmFunctionDebugMetadata);
    }


    private void ProcessFunctionParametersDeclaration(LLVMValueRef function, VariableSymbol[] parameters)
    {
        for (var i = 0; i < parameters.Length; i++)
        {
            var parameter = parameters[i];

            var llvmValue = function.GetParam((uint)i);
            var llvmType = parameter.Type!.TypeToLLVMType();

            parameter.LLVMReference = Builder.BuildAlloca(llvmType, parameter.Name);
            parameter.LLVMType = llvmType;
            // TODO: debug for parameter

            Builder.BuildStore(llvmValue, parameter.LLVMReference.Value);
        }
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

        UnreachableCode();
    }




    public void ProcessBlock(BoundBlockStatement statement)
        => ProcessScopeBlock(statement);


    private void ProcessScopeBlock(BoundBlockStatement statement, LLVMMetadataRef? function = null)
    {
        var oldScope = Scope;
        Scope = statement.Scope;

        DebugGenerateScope(statement.Scope, statement.Source(), function);

        // If a return statement is reached, the subsequent code after the return that is inside the same scope
        // will never be reached, so everything after it can be safely ignored. Also, LLVM doesn't compile
        // if it finds any code after a terminator.
        try
        {
            foreach (var subStatement in statement.Statements)
                Process(subStatement);
        }
        catch (UnreachableCodeControl)
        {}

        Scope = oldScope;
    }








    public LLVMValueRef Process(BoundExpression expression)
        => expression.Process(this);




    public LLVMValueRef ProcessLiteral(BoundLiteralExpression expression)
    {
        var llvmType = expression.Type!.TypeToLLVMType();
        var value = expression.Value!.Value;

        var llvmReference = LLVMValueRef.CreateConstInt(llvmType, value);

        return llvmReference;
    }




    public LLVMValueRef ProcessBinary(BoundBinaryExpression expression)
    {
        var syntax = (expression.Syntax as BinaryExpression)!;

        var left = Process(expression.Right);
        var right = Process(expression.Left);

        return syntax.Operator.Type switch
        {
            TokenType.Plus => Builder.BuildAdd(left, right, "sum"),
            TokenType.Minus => Builder.BuildSub(left, right, "sub"),
            TokenType.Star => Builder.BuildMul(left, right, "mult"),
            TokenType.Slash => Builder.BuildSDiv(left, right, "div"),

            _ => throw new UnreachableException()
        };
    }




    public LLVMValueRef ProcessUnary(BoundUnaryExpression expression)
    {
        var syntax = (expression.Syntax as UnaryExpression)!;

        var value = Process(expression.Expression);
        var llvmType = expression.Type!.TypeToLLVMType();

        return syntax.Operator.Type switch
        {
            TokenType.Minus => Builder.BuildSub(LLVMValueRef.CreateConstInt(llvmType, 0), value, "ineg"),
            TokenType.Exclamation => Builder.BuildXor(value, LLVMValueRef.CreateConstInt(llvmType, 1), "bneg"),
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

        if (expression.GetAddress || symbol is FunctionSymbol)
            return llvmReference;

        return Builder.BuildLoad2(llvmType, llvmReference, "symval");
    }




    public LLVMValueRef ProcessAssignment(BoundAssignmentExpression expression)
    {
        var reference = Process(expression.Reference);
        var value = Process(expression.Value);

        var result = Builder.BuildStore(value, reference);
        //DebugUpdateLocalVariableValue(reference, expression.Source());

        return result;
    }


    public LLVMValueRef ProcessAssignmentReference(BoundAssignmentReferenceExpression expression) => expression.Reference switch
    {
        BoundSymbolExpression symbol => symbol.Symbol.LLVMReference!.Value,
        BoundPointerAccessExpression pointer => Process(pointer.Pointer),

        _ => throw new UnreachableException()
    };




    public LLVMValueRef ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        var value = Process(expression.Pointer);
        var llvmType = expression.Type!.TypeToLLVMType();

        return Builder.BuildLoad2(llvmType, value, "ptraccess");
    }




    public LLVMValueRef ProcessCall(BoundCallExpression expression)
    {
        var function = Process(expression.Callee);
        var arguments = expression.Arguments.Select(Process).ToArray();

        var llvmFunctionType = (expression.Callee.Type as FunctionType)!.FunctionTypeToLLVMType(false);

        return Builder.BuildCall2(llvmFunctionType, function, arguments, "retval");
    }




    public LLVMValueRef ProcessCast(BoundCastExpression expression)
    {
        var value = Process(expression.Value);

        var valueType = expression.Value.Type!;
        var toType = expression.Type!;

        var llvmValueType = value.TypeOf;
        var llvmToType = expression.Type!.TypeToLLVMType();

        var sourceTypeSize = SizeOf(llvmValueType);
        var targetTypeSize = SizeOf(llvmToType);


        // int to pointer... is this really useful?
        if (!valueType.IsPointer && toType.IsPointer)
            return Builder.BuildIntToPtr(value, llvmToType, "itopcast");

        // pointer to int
        if (valueType.IsPointer && !toType.IsPointer)
            return Builder.BuildPtrToInt(value, llvmToType, "ptoicast");

        // pointer to another pointer type
        if (valueType.IsPointer && toType.IsPointer)
            return Builder.BuildPointerCast(value, llvmToType, "ptrcast");


        // cast to higher
        if (sourceTypeSize < targetTypeSize)
            return Builder.BuildIntCast(value, llvmToType, "incrcast");

        // cast to lower
        if (sourceTypeSize > targetTypeSize)
            return Builder.BuildTrunc(value, llvmToType, "decrcast");


        return value;
    }
}
