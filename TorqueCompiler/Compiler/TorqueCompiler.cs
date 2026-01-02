// ReSharper disable LocalizableElement


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

using LLVMSharp.Interop;


namespace Torque.Compiler;




public class TorqueCompiler : IBoundStatementProcessor, IBoundExpressionProcessor<LLVMValueRef>
{
    private const string FunctionEntryBlockName = "entry";


    private static LLVMValueRef Zero { get; } = NewInteger(0);
    private static LLVMValueRef One { get; } = NewInteger(1);


    // if debug metadata generation is desired, this property must be set, since
    // debug metadata uses some file information
    public FileInfo? File { get; }


    private readonly LLVMModuleRef _module = LLVMModuleRef.CreateWithName("MainModule");
    public LLVMModuleRef Module => _module;

    public LLVMBuilderRef Builder { get; } = LLVMBuilderRef.Create(LLVMContextRef.Global);

    public TargetMachine TargetMachine { get; }
    public LLVMTargetDataRef DataLayout => TargetMachine.DataLayout;

    public DebugMetadataGenerator? Debug { get; }

    private LLVMValueRef? _currentFunction;


    public IReadOnlyList<BoundStatement> Statements { get; }

    public Scope GlobalScope { get; }

    private Scope _scope = null!;
    public Scope Scope
    {
        get => _scope;
        private init => _scope = value;
    }




    public TorqueCompiler(IReadOnlyList<BoundStatement> statements, Scope globalScope, FileInfo? file = null, bool generateDebugMetadata = false)
    {
        // TODO: add optimization command line options (later... this is more useful after this language is able to do more stuff)
        // TODO: add importing system

        // TODO: add arrays

        TargetMachine = TargetMachine.Global ?? throw new InvalidOperationException("The global target machine instance must be initialized");
        _module.Target = TargetMachine.Triple;
        _module.DataLayout = TargetMachine.StringDataLayout;

        File = file;


        GlobalScope = globalScope;
        Scope = GlobalScope;

        if (generateDebugMetadata)
            Debug = new DebugMetadataGenerator(this);

        Statements = statements.ToArray();
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

        var reference = Builder.BuildAlloca(llvmType, $"var.${name}");
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
        ProcessFunctionBlock(statement.Body, symbol);
        _currentFunction = null;
    }


    private void DeclareFunctionParameters(LLVMValueRef function, IReadOnlyList<VariableSymbol> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            var type = parameter.Type!;

            var llvmValue = function.GetParam((uint)i);
            var llvmType = type.TypeToLLVMType();
            var llvmReference = Builder.BuildAlloca(llvmType, $"param.${parameter.Name}");
            var llvmDebugMetadata = DebugGenerateParameter(parameter.Name, i + 1, type, parameter.Location, llvmReference);

            parameter.SetLLVMProperties(llvmReference, llvmType, llvmDebugMetadata);

            Builder.BuildStore(llvmValue, llvmReference);
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


    private void ProcessFunctionBlock(BoundBlockStatement statement, FunctionSymbol function)
        => Scope.ProcessInnerScope(ref _scope, statement.Scope, () =>
        {
            DebugGenerateScope(Scope, statement.Location(), function.LLVMDebugMetadata);
            DeclareFunctionParameters(function.LLVMReference!.Value, function.Parameters);
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
        var type = expression.Type!;
        var llvmType = type.TypeToLLVMType();
        var value = expression.Value!;

        var llvmReference = type.Base.Type switch
        {
            _ when type.IsInteger => NewInteger((ulong)value, llvmType),
            _ when type.IsFloat => NewReal((double)value, llvmType),

            _ => throw new UnreachableException()
        };

        return llvmReference;
    }




    public LLVMValueRef ProcessBinary(BoundBinaryExpression expression)
    {
        var left = Process(expression.Right);
        var right = Process(expression.Left);

        var leftType = expression.Left.Type!;

        return leftType switch
        {
            _ when !leftType.IsFloat => ProcessIntegerBinaryOperation(expression.Syntax.Operator.Type, left, right, leftType.IsSigned),
            _ when leftType.IsFloat => ProcessFloatBinaryOperation(expression.Syntax.Operator.Type, left, right),

            _ => throw new UnreachableException()
        };
    }


    private LLVMValueRef ProcessIntegerBinaryOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right, bool isSigned = true) => @operator switch
    {
        TokenType.Plus => Builder.BuildAdd(left, right, "sum.int"),
        TokenType.Minus => Builder.BuildSub(left, right, "sub.int"),
        TokenType.Star => Builder.BuildMul(left, right, "mult.int"),
        TokenType.Slash when isSigned => Builder.BuildSDiv(left, right, "div.signed"),
        TokenType.Slash when !isSigned => Builder.BuildUDiv(left, right, "div.unsigned"),

        _ => throw new UnreachableException() // TODO: use UnreachableException everywhere in cases like this
    };


    private LLVMValueRef ProcessFloatBinaryOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right) => @operator switch
    {
        TokenType.Plus => Builder.BuildFAdd(left, right, "sum.float"),
        TokenType.Minus => Builder.BuildFSub(left, right, "sub.float"),
        TokenType.Star => Builder.BuildFMul(left, right, "mult.float"),
        TokenType.Slash => Builder.BuildFDiv(left, right, "div.float"),

        _ => throw new UnreachableException()
    };




    public LLVMValueRef ProcessUnary(BoundUnaryExpression expression)
    {
        var value = Process(expression.Expression);
        var type = expression.Type!;
        var llvmType = type.TypeToLLVMType();

        var operation = expression.Syntax.Operator.Type;

        return operation switch
        {
            TokenType.Minus when !type.IsFloat => ProcessIntegerNegateOperation(llvmType, value),
            TokenType.Minus when type.IsFloat => ProcessFloatNegateOperation(llvmType, value),
            TokenType.Exclamation => ProcessBooleanNegateOperation(llvmType, value),

            _ => throw new UnreachableException()
        };
    }


    private LLVMValueRef ProcessIntegerNegateOperation(LLVMTypeRef llvmType, LLVMValueRef value)
    {
        var constIntZero = NewInteger(0, llvmType);
        return Builder.BuildSub(constIntZero, value, "negate.int");
    }


    private LLVMValueRef ProcessFloatNegateOperation(LLVMTypeRef llvmType, LLVMValueRef value)
    {
        var constFloatZero = NewReal(0, llvmType);
        return Builder.BuildFSub(constFloatZero, value, "negate.float");
    }


    private LLVMValueRef ProcessBooleanNegateOperation(LLVMTypeRef llvmType, LLVMValueRef value)
    {
        var constIntOne = NewInteger(1, llvmType);
        return Builder.BuildXor(value, constIntOne, "negate.bool");
    }




    public LLVMValueRef ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public LLVMValueRef ProcessComparison(BoundComparisonExpression expression)
    {
        var left = Process(expression.Left);
        var right = Process(expression.Right);

        var leftType = expression.Left.Type!;

        var @operator = expression.Syntax.Operator.Type;
        var isSigned = expression.Type.IsSigned;

        return leftType switch
        {
            _ when !leftType.IsFloat => ProcessIntegerComparisonOperation(@operator, left, right, isSigned),
            _ when leftType.IsFloat => ProcessFloatComparisonOperation(@operator, left, right),

            _ => throw new UnreachableException()
        };
    }


    private LLVMValueRef ProcessIntegerComparisonOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right, bool signed = false)
    {
        var operation = @operator switch
        {
            TokenType.GreaterThan => !signed ? LLVMIntPredicate.LLVMIntUGT : LLVMIntPredicate.LLVMIntSGT,
            TokenType.LessThan => !signed ? LLVMIntPredicate.LLVMIntULT : LLVMIntPredicate.LLVMIntSLT,
            TokenType.GreaterThanOrEqual => !signed ? LLVMIntPredicate.LLVMIntUGE : LLVMIntPredicate.LLVMIntSGE,
            TokenType.LessThanOrEqual => !signed ? LLVMIntPredicate.LLVMIntULT : LLVMIntPredicate.LLVMIntSLE,

            _ => throw new UnreachableException()
        };

        return Builder.BuildICmp(operation, left, right, "compare.int");
    }


    private LLVMValueRef ProcessFloatComparisonOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right)
    {
        var operation = @operator switch
        {
            TokenType.GreaterThan => LLVMRealPredicate.LLVMRealOGT,
            TokenType.LessThan => LLVMRealPredicate.LLVMRealOLT,
            TokenType.GreaterThanOrEqual => LLVMRealPredicate.LLVMRealOGE,
            TokenType.LessThanOrEqual => LLVMRealPredicate.LLVMRealOLE,

            _ => throw new UnreachableException()
        };

        return Builder.BuildFCmp(operation, left, right, "compare.float");
    }




    public LLVMValueRef ProcessEquality(BoundEqualityExpression expression)
    {
        var left = Process(expression.Left);
        var right = Process(expression.Right);

        var leftType = expression.Left.Type!;

        return leftType switch
        {
            _ when !leftType.IsFloat => ProcessIntegerEqualityOperation(expression.Syntax.Operator.Type, left, right),
            _ when leftType.IsFloat => ProcessFloatEqualityOperation(expression.Syntax.Operator.Type, left, right),

            _ => throw new UnreachableException()
        };
    }


    private LLVMValueRef ProcessIntegerEqualityOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right)
    {
        var operation = @operator switch
        {
            TokenType.Equality => LLVMIntPredicate.LLVMIntEQ,
            TokenType.Inequality => LLVMIntPredicate.LLVMIntNE,

            _ => throw new UnreachableException()
        };

        return Builder.BuildICmp(operation, left, right, "compare.int");
    }


    private LLVMValueRef ProcessFloatEqualityOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right)
    {
        var operation = @operator switch
        {
            TokenType.Equality => LLVMRealPredicate.LLVMRealOEQ,
            TokenType.Inequality => LLVMRealPredicate.LLVMRealONE,

            _ => throw new UnreachableException()
        };

        return Builder.BuildFCmp(operation, left, right, "compare.float");
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

        var leftThenBlock = isLogicAnd ? rhsBlock : trueBlock; // where to go if operation is "and" and left operand is true
        var leftElseBlock = isLogicAnd ? falseBlock : rhsBlock; // where to go if operation is "and" and left operand if false


        Builder.BuildCondBr(Process(expression.Left), leftThenBlock, leftElseBlock);

        Builder.PositionAtEnd(rhsBlock);
        Builder.BuildCondBr(Process(expression.Right), trueBlock, falseBlock);

        Builder.PositionAtEnd(trueBlock);
        Builder.BuildBr(joinBlock);

        Builder.PositionAtEnd(falseBlock);
        Builder.BuildBr(joinBlock);

        Builder.PositionAtEnd(joinBlock);

        var phi = Builder.BuildPhi(LLVMTypeRef.Int1, "logic.result");
        phi.AddIncoming([NewBoolean(true), NewBoolean(false)], [trueBlock, falseBlock], 2);


        return phi;
    }




    public LLVMValueRef ProcessSymbol(BoundSymbolExpression expression)
    {
        var symbol = expression.Symbol;

        var llvmReference = symbol.LLVMReference!.Value;
        var llvmType = symbol.LLVMType!.Value;

        // Sometimes, we want the symbol memory address instead of its value.
        if (expression.GetAddress || symbol is FunctionSymbol)
            return llvmReference;

        return Builder.BuildLoad2(llvmType, llvmReference, "symbol.value");
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

        return Builder.BuildLoad2(llvmType, value, "access.ptr");
    }




    public LLVMValueRef ProcessCall(BoundCallExpression expression)
    {
        var function = Process(expression.Callee);
        var arguments = ProcessAll(expression.Arguments.AsReadOnly());

        var functionType = (expression.Callee.Type as FunctionType)!;
        var llvmFunctionType = functionType.FunctionTypeToLLVMType(false);

        return Builder.BuildCall2(llvmFunctionType, function, arguments.ToArray(), "return.value");
    }




    public LLVMValueRef ProcessCast(BoundCastExpression expression)
    {
        var value = Process(expression.Value);

        var from = expression.Value.Type!;
        var to = expression.Type!;

        return Cast(from, to, value);
    }




    public LLVMValueRef ProcessImplicitCast(BoundImplicitCastExpression expression)
    {
        var value = Process(expression.Value);

        var from = expression.Value.Type!;
        var to = expression.Type!;

        return Cast(from, to, value);
    }




    public LLVMValueRef ProcessArray(BoundArrayExpression expression)
    {
        var llvmArrayType = expression.Type!.TypeToLLVMType();
        var elements = expression.Elements.Select(Process).ToArray();

        var array = Builder.BuildAlloca(llvmArrayType, "array");
        var address = Builder.BuildGEP2(llvmArrayType, array, new[] { Zero, Zero }, "array.address");

        for (var i = 0; i < elements.Length; i++)
        {
            var elementAddress = Builder.BuildGEP2(llvmArrayType, array, [Zero, NewInteger((ulong)i)]);
            Builder.BuildStore(elements[i], elementAddress);
        }

        return address;
    }

    #endregion








    #region Generation Methods

    private LLVMValueRef Cast(Type from, Type to, LLVMValueRef value)
    {
        var llvmFrom = from.TypeToLLVMType();
        var llvmTo = to.TypeToLLVMType();

        var sourceTypeSize = SizeOf(llvmFrom);
        var targetTypeSize = SizeOf(llvmTo);

        return (valueType: from, toType: to) switch
        {
            _ when !from.IsPointer && to.IsPointer => Builder.BuildIntToPtr(value, llvmTo, "cast.int->ptr"), // int to pointer... is this really useful?
            _ when from.IsPointer && !to.IsPointer => Builder.BuildPtrToInt(value, llvmTo, "cast.ptr->int"), // pointer to int
            _ when from.IsPointer && to.IsPointer => Builder.BuildPointerCast(value, llvmTo, "cast.ptr"), // pointer to another pointer type

            _ when from.IsFloat && !to.IsFloat && to.IsSigned => Builder.BuildFPToSI(value, llvmTo, "cast.float->int"), // float to int
            _ when from.IsFloat && !to.IsFloat && !to.IsSigned => Builder.BuildFPToUI(value, llvmTo, "cast.float->uint"), // float to uint
            _ when !from.IsFloat && to.IsFloat && from.IsSigned => Builder.BuildSIToFP(value, llvmTo, "cast.int->float"), // int to float
            _ when !from.IsFloat && to.IsFloat && !from.IsSigned => Builder.BuildUIToFP(value, llvmTo, "cast.uint->float"), // uint to float
            _ when from.IsFloat && to.IsFloat => Builder.BuildFPCast(value, llvmTo, "cast.float"), // float to another float type

            _ when sourceTypeSize != targetTypeSize => Builder.BuildIntCast(value, llvmTo, "cast.int"), // integer to another integer type

            _ => value // source type is the same as target type
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




    private LLVMMetadataRef? DebugGenerateLocalVariable(string name, Type type, SourceLocation location, LLVMValueRef alloca)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);

        if (llvmLocation is not null)
            return Debug?.GenerateLocalVariable(name, type, location.Line, alloca, llvmLocation.Value);

        return null;
    }




    private LLVMMetadataRef? DebugGenerateParameter(string name, int index, Type type, SourceLocation location, LLVMValueRef alloca)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);

        if (llvmLocation is not null)
            return Debug?.GenerateParameter(name, type, location.Line, index, alloca, llvmLocation.Value);

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
        => type.SizeOfThisInMemory(DataLayout);




    private static void UnreachableCode()
        => throw new UnreachableCodeControl();




    private static LLVMValueRef NewInteger(ulong value, LLVMTypeRef? type = null)
        => LLVMValueRef.CreateConstInt(type ?? LLVMTypeRef.Int32, value);


    private static LLVMValueRef NewReal(double value, LLVMTypeRef? type = null)
        => LLVMValueRef.CreateConstReal(type ?? LLVMTypeRef.Double, value);


    private static LLVMValueRef NewBoolean(bool value)
        => LLVMValueRef.CreateConstInt(LLVMTypeRef.Int1, value ? 1UL : 0UL);
}
