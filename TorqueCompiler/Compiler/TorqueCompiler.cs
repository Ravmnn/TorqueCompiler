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

    private LLVMValueRef? _currentFunction;


    public IReadOnlyList<BoundStatement> Statements { get; }

    public Scope GlobalScope { get; }

    private Scope _scope = null!;
    public Scope Scope
    {
        get => _scope;
        private set => _scope = value;
    }




    public TorqueCompiler(IReadOnlyList<BoundStatement> statements, Scope globalScope, FileInfo? file = null, bool generateDebugMetadata = false)
    {
        // TODO: add optimization command line options (later... this is more useful after this language is able to do more stuff)
        // TODO: add importing system

        // TODO: add arrays
        // TODO: add floats
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








    #region Statements

    public void Process(BoundStatement statement)
    {
        statement.Process(this);
    }




    public void ProcessExpression(BoundExpressionStatement statement)
    {
        DebugSetLocationTo(statement.Location());
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


        DebugSetLocationTo(statementSource.Location);

        var reference = Builder.BuildAlloca(llvmType, name);
        var debugReference = DebugGenerateLocalVariable(name, type, statementSource, reference);

        symbol.SetLLVMProperties(reference, llvmType, debugReference);

        Builder.BuildStore(Process(statement.Value), reference);

        DebugSetLocationTo(null);
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
    {
        var symbol = statement.Symbol;

        var functionName = symbol.Name;
        var functionReturnType = symbol.Type!.ReturnType;
        var functionLocation = statement.Location();
        var parameters = statement.Symbol.Parameters;
        var parameterTypes = symbol.Type.ParametersType;

        var llvmFunctionReturnType = functionReturnType.TypeToLLVMType();
        var llvmParameterTypes = parameterTypes.TypesToLLVMTypes();
        var llvmFunctionType = LLVMTypeRef.CreateFunction(llvmFunctionReturnType, llvmParameterTypes.ToArray());
        var llvmFunctionReference = Module.AddFunction(functionName, llvmFunctionType);
        var llvmFunctionDebugMetadata = DebugGenerateFunction(llvmFunctionReference, functionName, functionLocation, symbol.Type);

        symbol.SetLLVMProperties(llvmFunctionReference, llvmFunctionType, llvmFunctionDebugMetadata);


        var entry = llvmFunctionReference.AppendBasicBlock(FunctionEntryBlockName);
        Builder.PositionAtEnd(entry);

        _currentFunction = llvmFunctionReference;

        DeclareFunctionParameters(llvmFunctionReference, parameters);
        ProcessFunctionBlock(statement.Body, llvmFunctionDebugMetadata);

        _currentFunction = null;
    }


    private void DeclareFunctionParameters(LLVMValueRef function, IReadOnlyList<VariableSymbol> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
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
            DebugSetLocationTo(statement.Location());
            Builder.BuildRet(Process(statement.Expression));
            DebugSetLocationTo(null);
        }
        else
            Builder.BuildRetVoid();

        UnreachableCode();
    }




    public void ProcessBlock(BoundBlockStatement statement)
        => ProcessLexicalBlock(statement);


    private void ProcessLexicalBlock(BoundBlockStatement statement)
        => Scope.ProcessInnerScope(ref _scope, statement.Scope, () =>
        {
            DebugGenerateScope(Scope, statement.Location());
            ProcessBlockWithControl(statement);
        });


    private void ProcessFunctionBlock(BoundBlockStatement statement, LLVMMetadataRef? function)
        => Scope.ProcessInnerScope(ref _scope, statement.Scope, () =>
        {
            DebugGenerateScope(Scope, statement.Location(), function);
            ProcessBlockWithControl(statement);
        });


    private void ProcessBlockWithControl(BoundBlockStatement statement)
    {
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
    }

    #endregion








    #region Expressions

    public LLVMValueRef Process(BoundExpression expression)
        => expression.Process(this);


    public IReadOnlyList<LLVMValueRef> ProcessAll(IReadOnlyList<BoundExpression> expressions)
        => expressions.Select(Process).ToArray();




    public LLVMValueRef ProcessLiteral(BoundLiteralExpression expression)
    {
        var llvmType = expression.Type!.TypeToLLVMType();
        var value = expression.Value!.Value;

        var llvmReference = LLVMValueRef.CreateConstInt(llvmType, value);

        return llvmReference;
    }




    public LLVMValueRef ProcessBinary(BoundBinaryExpression expression)
    {
        var left = Process(expression.Right);
        var right = Process(expression.Left);

        return ProcessBinaryOperation(expression.Syntax.Operator.Type, left, right);
    }


    private LLVMValueRef ProcessBinaryOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right) => @operator switch
    {
        TokenType.Plus => Builder.BuildAdd(left, right, "sum"),
        TokenType.Minus => Builder.BuildSub(left, right, "sub"),
        TokenType.Star => Builder.BuildMul(left, right, "mult"),
        TokenType.Slash => Builder.BuildSDiv(left, right, "div"),

        _ => throw new UnreachableException()
    };




    public LLVMValueRef ProcessUnary(BoundUnaryExpression expression)
    {
        var value = Process(expression.Expression);
        var llvmType = expression.Type!.TypeToLLVMType();

        return ProcessUnaryOperation(expression.Syntax.Operator.Type, llvmType, value);
    }


    private LLVMValueRef ProcessUnaryOperation(TokenType @operator, LLVMTypeRef llvmType, LLVMValueRef value)
    {
        var constZero = LLVMValueRef.CreateConstInt(llvmType, 0);
        var constOne = LLVMValueRef.CreateConstInt(llvmType, 1);

        return @operator switch
        {
            TokenType.Minus => Builder.BuildSub(constZero, value, "ineg"), // integer
            TokenType.Exclamation => Builder.BuildXor(value, constOne, "bneg"), // boolean

            _ => throw new UnreachableException()
        };
    }




    public LLVMValueRef ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public LLVMValueRef ProcessComparison(BoundComparisonExpression expression)
    {
        var left = Process(expression.Left);
        var right = Process(expression.Right);

        var @operator = expression.Syntax.Operator.Type;
        var isUnsigned = expression.Type.IsUnsigned;

        return ProcessComparisonOperation(@operator, left, right, isUnsigned);
    }


    private LLVMValueRef ProcessComparisonOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right, bool unsigned = false)
    {
        var operation = @operator switch
        {
            TokenType.GreaterThan => unsigned ? LLVMIntPredicate.LLVMIntUGT : LLVMIntPredicate.LLVMIntSGT,
            TokenType.LessThan => unsigned ? LLVMIntPredicate.LLVMIntULT : LLVMIntPredicate.LLVMIntSLT,
            TokenType.GreaterThanOrEqual => unsigned ? LLVMIntPredicate.LLVMIntUGE : LLVMIntPredicate.LLVMIntSGE,
            TokenType.LessThanOrEqual => unsigned ? LLVMIntPredicate.LLVMIntULT : LLVMIntPredicate.LLVMIntSLE,

            _ => throw new UnreachableException()
        };

        return Builder.BuildICmp(operation, left, right, "intcmp");
    }




    public LLVMValueRef ProcessEquality(BoundEqualityExpression expression)
    {
        var left = Process(expression.Left);
        var right = Process(expression.Right);

        return ProcessEqualityOperation(expression.Syntax.Operator.Type, left, right);
    }


    private LLVMValueRef ProcessEqualityOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right)
    {
        var operation = @operator switch
        {
            TokenType.Equality => LLVMIntPredicate.LLVMIntEQ,
            TokenType.Inequality => LLVMIntPredicate.LLVMIntNE,

            _ => throw new UnreachableException()
        };

        return Builder.BuildICmp(operation, left, right, "intcmp");
    }




    public LLVMValueRef ProcessLogic(BoundLogicExpression expression)
    {
        // due to LLVM's SSA system, the use of "phi" is needed to implement a logic expression

        var rhsBlock = _currentFunction!.Value.AppendBasicBlock("rhs");
        var trueBlock = _currentFunction!.Value.AppendBasicBlock("true");
        var falseBlock = _currentFunction!.Value.AppendBasicBlock("false");
        var joinBlock = _currentFunction!.Value.AppendBasicBlock("join");

        var operation = expression.Syntax.Operator.Type;
        var isLogicAnd = operation == TokenType.LogicAnd;

        if (operation is not TokenType.LogicAnd and not TokenType.LogicOr)
            throw new UnreachableException();

        var leftThenBlock = isLogicAnd ? rhsBlock : trueBlock;
        var leftElseBlock = isLogicAnd ? falseBlock : rhsBlock;


        Builder.BuildCondBr(Process(expression.Left), leftThenBlock, leftElseBlock);

        Builder.PositionAtEnd(rhsBlock);
        Builder.BuildCondBr(Process(expression.Right), trueBlock, falseBlock);

        Builder.PositionAtEnd(trueBlock);
        Builder.BuildBr(joinBlock);

        Builder.PositionAtEnd(falseBlock);
        Builder.BuildBr(joinBlock);

        Builder.PositionAtEnd(joinBlock);

        var phi = Builder.BuildPhi(LLVMTypeRef.Int1, "logicres");
        phi.AddIncoming([NewBoolean(true), NewBoolean(false)], [trueBlock, falseBlock], 2);


        return phi;
    }


    private static LLVMValueRef NewBoolean(bool value)
        => LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, value ? 1UL : 0UL);




    public LLVMValueRef ProcessSymbol(BoundSymbolExpression expression)
    {
        var symbol = expression.Symbol;

        var llvmReference = symbol.LLVMReference!.Value;
        var llvmType = symbol.LLVMType!.Value;

        // Sometimes, we want the symbol memory address instead of its value.
        if (expression.GetAddress || symbol is FunctionSymbol)
            return llvmReference;

        return Builder.BuildLoad2(llvmType, llvmReference, "symval");
    }




    public LLVMValueRef ProcessAssignment(BoundAssignmentExpression expression)
    {
        var reference = Process(expression.Reference);
        var value = Process(expression.Value);

        Builder.BuildStore(value, reference);

        return value;
    }


    public LLVMValueRef ProcessAssignmentReference(BoundAssignmentReferenceExpression expression) => expression.Reference switch
    {
        BoundSymbolExpression symbol => symbol.Symbol.LLVMReference!.Value, // if it is a symbol, we want its memory address
        BoundPointerAccessExpression pointer => Process(pointer.Pointer), // if it is a pointer, we want the memory address stored by it

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
        var arguments = ProcessAll(expression.Arguments);

        var functionType = (expression.Callee.Type as FunctionType)!;
        var llvmFunctionType = functionType.FunctionTypeToLLVMType(false);

        return Builder.BuildCall2(llvmFunctionType, function, arguments.ToArray(), "retval");
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


        return (valueType, toType) switch
        {
            _ when !valueType.IsPointer && toType.IsPointer => Builder.BuildIntToPtr(value, llvmToType, "itopcast"), // int to pointer... is this really useful?
            _ when valueType.IsPointer && !toType.IsPointer => Builder.BuildPtrToInt(value, llvmToType, "ptoicast"), // pointer to int
            _ when valueType.IsPointer && toType.IsPointer => Builder.BuildPointerCast(value, llvmToType, "ptrcast"), // pointer to another pointer type

            _ when sourceTypeSize < targetTypeSize => Builder.BuildIntCast(value, llvmToType, "incrcast"), // cast to higher
            _ when sourceTypeSize > targetTypeSize => Builder.BuildTrunc(value, llvmToType, "decrcast"), // cast to lower

            _ => value
        };
    }

    #endregion








    #region Debug Metadata

    private LLVMMetadataRef? DebugSetLocationTo(SourceLocation? location)
    {
        if (location is null)
        {
            Debug?.SetLocation();
            return null;
        }

        return Debug?.SetLocation(location.Value.Line, location.Value.Start);
    }


    private LLVMMetadataRef? DebugCreateLocation(SourceLocation location)
        => Debug?.CreateDebugLocation(location.Line, location.Start);


    private LLVMMetadataRef? DebugCreateLexicalScope(SourceLocation location)
        => Debug?.CreateLexicalScope(location.Line, location.Start);




    private LLVMMetadataRef? DebugGenerateFunction(LLVMValueRef function, string functionName, SourceLocation functionLocation, FunctionType type)
        => Debug?.GenerateFunction(function, functionName, functionLocation.Line, type);




    private LLVMMetadataRef? DebugGenerateLocalVariable(string name, Type type, Token statementSource, LLVMValueRef alloca)
    {
        var location = statementSource.Location;
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);

        if (llvmLocation is not null)
            return Debug?.GenerateLocalVariable(name, type, location.Line, alloca, llvmLocation.Value);

        return null;
    }


    // These methods are only necessary when the variable for some reason does not have an alloca (memory address).
    // if the variable has an alloca, the debugger is able to track its memory address and, consequently
    // its value. Since currently a variable cannot be created without alloca, they're useless
    private LLVMDbgRecordRef? DebugUpdateLocalVariableValue(string name, SourceLocation location)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);
        return Debug?.UpdateLocalVariableValue(name, llvmLocation!.Value);
    }


    private LLVMDbgRecordRef? DebugUpdateLocalVariableValue(LLVMValueRef reference, SourceLocation location)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);
        return Debug?.UpdateLocalVariableValue(reference, llvmLocation!.Value);
    }




    private void DebugGenerateScope(Scope scope, SourceLocation location, LLVMMetadataRef? debugFunctionReference = null)
    {
        var llvmDebugScopeMetadata = DebugCreateLexicalScope(location);
        llvmDebugScopeMetadata = debugFunctionReference ?? llvmDebugScopeMetadata;

        scope.DebugMetadata = llvmDebugScopeMetadata;
    }

    #endregion




    private int SizeOf(LLVMTypeRef type)
        => type.SizeOfThis(TargetData);




    private static void UnreachableCode()
        => throw new UnreachableCodeControl();
}
