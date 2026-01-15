// ReSharper disable LocalizableElement


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LLVMSharp.Interop;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;


using Type = Torque.Compiler.Types.Type;


namespace Torque.Compiler;




public class TorqueCompiler : IBoundStatementProcessor, IBoundExpressionProcessor<LLVMValueRef>
{

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


    private IntrinsicCaller _intrinsics;


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
        File = file;

        TargetMachine = TargetMachine.Global ?? throw new InvalidOperationException("The global target machine instance must be initialized");
        _module.Target = TargetMachine.Triple;
        _module.DataLayout = TargetMachine.StringDataLayout;

        _intrinsics = new IntrinsicCaller(Module, Builder);

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
        DebugForLocationDo(statement.Location, () => Process(statement.Expression));
    }




    public void ProcessDeclaration(BoundDeclarationStatement statement)
    {
        DebugForLocationDo(statement.Location, () =>
        {
            var symbol = statement.Symbol;
            var reference = CreateVariableAlloca(symbol, $"var.${symbol.Name}");
            symbol.LLVMDebugMetadata = DebugGenerateLocalVariable(symbol);

            Builder.BuildStore(Process(statement.Value), reference);
        });
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
    {
        var functionSymbol = statement.Symbol;

        var llvmFunctionType = functionSymbol.Type!.ToRawLLVMType();
        var llvmReference = Module.AddFunction(functionSymbol.Name, llvmFunctionType);
        llvmReference.Linkage = LLVMLinkage.LLVMExternalLinkage;

        functionSymbol.SetLLVMProperties(llvmReference, llvmFunctionType, null);
        functionSymbol.LLVMDebugMetadata = DebugGenerateFunction(functionSymbol);

        ProcessFunctionBodyIfNotExternal(statement.Body, functionSymbol);
    }


    private void ProcessFunctionBodyIfNotExternal(BoundBlockStatement? body, FunctionSymbol functionSymbol)
    {
        if (body is null)
            return;

        var reference = functionSymbol.LLVMReference!.Value;
        var entry = reference.AppendBasicBlock("entry");
        Builder.PositionAtEnd(entry);

        _currentFunction = reference;
        ProcessFunctionBlock(body, functionSymbol);
        _currentFunction = null;
    }


    private void DeclareFunctionParameters(LLVMValueRef function, IReadOnlyList<VariableSymbol> parameters)
    {
        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];

            var llvmReference = CreateVariableAlloca(parameter, $"param.${parameter.Name}");
            parameter.LLVMDebugMetadata = DebugGenerateParameter(parameter, i + 1);

            var llvmValue = function.GetParam((uint)i);
            Builder.BuildStore(llvmValue, llvmReference);
        }
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        if (statement.Expression is not null)
            DebugForLocationDo(statement.Location, () => Builder.BuildRet(Process(statement.Expression)));
        else
            Builder.BuildRetVoid();

        UnreachableCode();
    }




    public void ProcessBlock(BoundBlockStatement statement)
        => ProcessLexicalBlock(statement);


    private void ProcessLexicalBlock(BoundBlockStatement statement)
        => Scope.ForInnerScopeDo(ref _scope, statement.Scope, () =>
        {
            DebugGenerateScope(Scope, statement.Location);
            ProcessBlockWithControl(statement);
        });


    private void ProcessFunctionBlock(BoundBlockStatement statement, FunctionSymbol function)
        => Scope.ForInnerScopeDo(ref _scope, statement.Scope, () =>
        {
            DebugGenerateScope(Scope, statement.Location, function.LLVMDebugMetadata);
            DeclareFunctionParameters(function.LLVMReference!.Value, function.Parameters);
            ProcessBlockWithControl(statement, true);
        });


    private void ProcessBlockWithControl(BoundBlockStatement statement, bool addVoidReturnAtEnd = false)
    {
        // If a return statement is reached, the subsequent code after the return that is inside the same scope
        // will never be reached, so everything after it can be safely ignored. Also, LLVM doesn't compile
        // if it finds any code after a terminator.

        TryCatchControlException(() =>
        {
            foreach (var subStatement in statement.Statements)
                Process(subStatement);

            if (addVoidReturnAtEnd)
                Builder.BuildRetVoid();
        });
    }




    public unsafe void ProcessIf(BoundIfStatement statement)
    {
        var thenBlock = LLVM.CreateBasicBlockInContext(Module.Context, "then".StringToSBytePtr());
        var elseBlock = LLVM.CreateBasicBlockInContext(Module.Context, "else".StringToSBytePtr());
        var joinBlock = LLVM.CreateBasicBlockInContext(Module.Context, "join".StringToSBytePtr());

        var hasElse = statement.ElseStatement is not null;

        var condition = Process(statement.Condition);


        DebugForLocationDo(statement.Location, () =>
        {
            Builder.BuildCondBr(condition, thenBlock, hasElse ? elseBlock : joinBlock);

            ProcessBlockSuccessor(statement.ThenStatement, thenBlock, joinBlock);

            if (hasElse)
                ProcessBlockSuccessor(statement.ElseStatement!, elseBlock, joinBlock);
        });


        LLVM.AppendExistingBasicBlock(_currentFunction!.Value, joinBlock);
        Builder.PositionAtEnd(joinBlock);
    }


    private unsafe void ProcessBlockSuccessor(BoundStatement statement, LLVMOpaqueBasicBlock* block, LLVMOpaqueBasicBlock* join)
    {
        DebugSetLocationTo(statement.Location);

        LLVM.AppendExistingBasicBlock(_currentFunction!.Value, block);
        Builder.PositionAtEnd(block);
        TryCatchControlException(() => Process(statement));
        Builder.BuildBr(join);
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
        var llvmType = type.ToLLVMType();
        var value = expression.Value!;

        var llvmReference = ValueFromLiteral(type, value, llvmType);

        return llvmReference;
    }


    private LLVMValueRef ValueFromLiteral(Type type, object value, LLVMTypeRef llvmType) => type.Base.Type switch
    {
        _ when type.IsString => StringFromLiteral((value as IReadOnlyList<byte>)!),

        _ when type.IsChar => Constant.Integer((byte)value, LLVMTypeRef.Int8),
        _ when type.IsBool => Constant.Boolean((bool)value),
        _ when type.IsInteger => Constant.Integer((ulong)value, llvmType),
        _ when type.IsFloat => Constant.Real((double)value, llvmType),

        _ => throw new UnreachableException()
    };


    private LLVMValueRef StringFromLiteral(IReadOnlyList<byte> @string)
    {
        var chars = Encoding.ASCII.GetChars(@string.ToArray());

        return Builder.BuildGlobalStringPtr(chars, "literal.string");
    }




    public LLVMValueRef ProcessBinary(BoundBinaryExpression expression)
    {
        var left = Process(expression.Left);
        var right = Process(expression.Right);

        var leftType = expression.Left.Type!;

        return ProcessBinaryOperation(expression, leftType, left, right);
    }


    private LLVMValueRef ProcessBinaryOperation(BoundBinaryExpression expression, Type leftType, LLVMValueRef left, LLVMValueRef right) => leftType switch
    {
        _ when leftType.IsInteger => ProcessIntegerBinaryOperation(expression.Syntax.Operator, left, right, leftType.IsSigned),
        _ when leftType.IsFloat => ProcessFloatBinaryOperation(expression.Syntax.Operator, left, right),

        _ => throw new UnreachableException()
    };


    private LLVMValueRef ProcessIntegerBinaryOperation(TokenType @operator, LLVMValueRef left, LLVMValueRef right, bool isSigned = true) => @operator switch
    {
        TokenType.Plus => Builder.BuildAdd(left, right, "sum.int"),
        TokenType.Minus => Builder.BuildSub(left, right, "sub.int"),
        TokenType.Star => Builder.BuildMul(left, right, "mult.int"),
        TokenType.Slash when isSigned => Builder.BuildSDiv(left, right, "div.signed"),
        TokenType.Slash when !isSigned => Builder.BuildUDiv(left, right, "div.unsigned"),

        _ => throw new UnreachableException()
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
        var llvmType = type.ToLLVMType();

        var operation = expression.Syntax.Operator;

        return ProcessUnaryOperation(operation, type, llvmType, value);
    }


    private LLVMValueRef ProcessUnaryOperation(TokenType operation, Type type, LLVMTypeRef llvmType, LLVMValueRef value) => operation switch
    {
        TokenType.Minus when type.IsInteger => ProcessIntegerNegateOperation(llvmType, value),
        TokenType.Minus when type.IsFloat => ProcessFloatNegateOperation(llvmType, value),
        TokenType.Exclamation => ProcessBooleanNegateOperation(llvmType, value),

        _ => throw new UnreachableException()
    };


    private LLVMValueRef ProcessIntegerNegateOperation(LLVMTypeRef llvmType, LLVMValueRef value)
    {
        var constIntZero = Constant.Integer(0, llvmType);
        return Builder.BuildSub(constIntZero, value, "negate.int");
    }


    private LLVMValueRef ProcessFloatNegateOperation(LLVMTypeRef llvmType, LLVMValueRef value)
    {
        var constFloatZero = Constant.Real(0, llvmType);
        return Builder.BuildFSub(constFloatZero, value, "negate.float");
    }


    private LLVMValueRef ProcessBooleanNegateOperation(LLVMTypeRef llvmType, LLVMValueRef value)
    {
        var constIntOne = Constant.Integer(1, llvmType);
        return Builder.BuildXor(value, constIntOne, "negate.bool");
    }




    public LLVMValueRef ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public LLVMValueRef ProcessComparison(BoundComparisonExpression expression)
    {
        var left = Process(expression.Left);
        var right = Process(expression.Right);

        var leftType = expression.Left.Type!;

        var @operator = expression.Syntax.Operator;
        var isSigned = expression.Type.IsSigned;

        return ProcessComparisonOperation(leftType, @operator, left, right, isSigned);
    }


    private LLVMValueRef ProcessComparisonOperation(Type leftType, TokenType @operator, LLVMValueRef left, LLVMValueRef right, bool isSigned) => leftType switch
    {
        _ when leftType.IsInteger => ProcessIntegerComparisonOperation(@operator, left, right, isSigned),
        _ when leftType.IsFloat => ProcessFloatComparisonOperation(@operator, left, right),

        _ => throw new UnreachableException()
    };


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

        return ProcessEqualityOperation(expression, leftType, left, right);
    }


    private LLVMValueRef ProcessEqualityOperation(BoundEqualityExpression expression, Type leftType, LLVMValueRef left, LLVMValueRef right) => leftType switch
    {
        _ when leftType.IsInteger => ProcessIntegerEqualityOperation(expression.Syntax.Operator, left, right),
        _ when leftType.IsFloat => ProcessFloatEqualityOperation(expression.Syntax.Operator, left, right),

        _ => throw new UnreachableException()
    };


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

        var operation = expression.Syntax.Operator;
        var isLogicAnd = operation == TokenType.LogicAnd;

        ThrowIfInvalidLogicOperation(operation);

        var leftThenBlock = isLogicAnd ? rhsBlock : trueBlock; // where to go if operation is "and" and left operand is true
        var leftElseBlock = isLogicAnd ? falseBlock : rhsBlock; // where to go if operation is "and" and left operand if false


        BuildLogicOperationOperandEvaluationBlocks(expression, leftThenBlock, leftElseBlock, rhsBlock, trueBlock, falseBlock);

        BuildLogicOperationValueBlock(trueBlock, joinBlock);
        BuildLogicOperationValueBlock(falseBlock, joinBlock);

        Builder.PositionAtEnd(joinBlock);


        return BuildLogicOperationPhi(trueBlock, falseBlock);
    }


    private void BuildLogicOperationOperandEvaluationBlocks(BoundLogicExpression expression, LLVMBasicBlockRef leftThenBlock,
        LLVMBasicBlockRef leftElseBlock, LLVMBasicBlockRef rhsBlock, LLVMBasicBlockRef trueBlock, LLVMBasicBlockRef falseBlock)
    {
        Builder.BuildCondBr(Process(expression.Left), leftThenBlock, leftElseBlock);

        Builder.PositionAtEnd(rhsBlock);
        Builder.BuildCondBr(Process(expression.Right), trueBlock, falseBlock);
    }


    private LLVMValueRef BuildLogicOperationPhi(LLVMBasicBlockRef trueBlock, LLVMBasicBlockRef falseBlock)
    {
        var phi = Builder.BuildPhi(LLVMTypeRef.Int1, "logic.result");
        phi.AddIncoming([Constant.Boolean(true), Constant.Boolean(false)], [trueBlock, falseBlock], 2);

        return phi;
    }


    private void BuildLogicOperationValueBlock(LLVMBasicBlockRef trueBlock, LLVMBasicBlockRef joinBlock)
    {
        Builder.PositionAtEnd(trueBlock);
        Builder.BuildBr(joinBlock);
    }


    private static void ThrowIfInvalidLogicOperation(TokenType operation)
    {
        if (operation is not TokenType.LogicAnd and not TokenType.LogicOr)
            throw new UnreachableException();
    }




    public LLVMValueRef ProcessSymbol(BoundSymbolExpression expression)
    {
        var symbol = expression.Symbol;

        var llvmReference = symbol.LLVMReference!.Value;
        var llvmType = symbol.LLVMType!.Value;

        // Sometimes, we want the symbol memory address instead of its value.
        if (symbol is FunctionSymbol)
            return llvmReference;

        return Builder.BuildLoad2(llvmType, llvmReference, "symbol.value");
    }




    public LLVMValueRef ProcessAddress(BoundAddressExpression expression)
        => Process(expression.Expression);


    public LLVMValueRef ProcessAddressable(BoundAddressableExpression expression) => expression.Expression switch
    {
        BoundSymbolExpression symbolExpression => symbolExpression.Symbol.LLVMReference!.Value,
        BoundIndexingExpression indexingExpression => ProcessIndexingExpression(indexingExpression, false),

        _ => throw new UnreachableException()
    };




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
        BoundIndexingExpression indexing => ProcessIndexingExpression(indexing, false),

        _ => throw new UnreachableException()
    };




    public LLVMValueRef ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        var value = Process(expression.Pointer);
        var llvmType = expression.Type!.ToLLVMType();

        return Builder.BuildLoad2(llvmType, value, "access.ptr");
    }




    public LLVMValueRef ProcessCall(BoundCallExpression expression)
    {
        var function = Process(expression.Callee);
        var arguments = ProcessAll(expression.Arguments.AsReadOnly());

        var functionType = (expression.Callee.Type as FunctionType)!;
        var llvmFunctionType = functionType.ToRawLLVMType();

        return Call(function, llvmFunctionType, arguments);
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
        // TODO: a string ("text") must be a constant to improve speed, since its value is always known at compile-time

        var llvmArrayType = expression.ArrayType!.ToLLVMType();
        var arrayAddress = Builder.BuildAlloca(llvmArrayType, "array.address");

        InitializeArrayElements(expression, llvmArrayType, arrayAddress);
        var firstElementAddress = IndexArray(llvmArrayType, arrayAddress, Constant.Zero);

        return firstElementAddress;
    }


    private void InitializeArrayElements(BoundArrayExpression expression, LLVMTypeRef llvmArrayType, LLVMValueRef arrayAddress)
    {
        var initializationList = expression.Elements?.Select(Process).ToArray() ?? [];

        InitializeArrayFromInitializationList(llvmArrayType, arrayAddress, initializationList);

        if ((ulong)initializationList.Length < expression.Syntax.Length)
            InitializeRemainingArrayElements((expression.ArrayType as ArrayType)!, llvmArrayType, arrayAddress, (ulong)initializationList.Length);
    }


    private void InitializeArrayFromInitializationList(LLVMTypeRef llvmArrayType, LLVMValueRef arrayAddress, LLVMValueRef[] llvmElements)
    {
        for (var i = 0; i < llvmElements.Length; i++)
        {
            var elementAddress = IndexArray(llvmArrayType, arrayAddress, Constant.Integer((ulong)i));
            Builder.BuildStore(llvmElements[i], elementAddress);
        }
    }


    private void InitializeRemainingArrayElements(ArrayType arrayType, LLVMTypeRef llvmArrayType, LLVMValueRef arrayAddress, ulong initializationListLength)
    {
        var elementTypeSize = (ulong)arrayType.Type.SizeOfTypeInMemory();
        var startAddress = IndexArray(llvmArrayType, arrayAddress, Constant.Integer(initializationListLength));

        var remainingSizeInBytes = GetRemainingEmptyBytesOfArray(arrayType.Length, initializationListLength, elementTypeSize);
        var remainingSizeInBytesValue = Constant.Integer(remainingSizeInBytes, LLVMTypeRef.Int64);

        _intrinsics.CallMemsetToZero(startAddress, remainingSizeInBytesValue);
    }


    private static ulong GetRemainingEmptyBytesOfArray(ulong arrayLength, ulong initializationListLength, ulong elementTypeSize)
    {
        var arraySizeInBytes = elementTypeSize * arrayLength;
        var elementsSizeInBytes = elementTypeSize * initializationListLength;
        var remainingSizeInBytes = arraySizeInBytes - elementsSizeInBytes;

        return remainingSizeInBytes;
    }




    public LLVMValueRef ProcessIndexing(BoundIndexingExpression expression)
        => ProcessIndexingExpression(expression);


    private LLVMValueRef ProcessIndexingExpression(BoundIndexingExpression expression, bool getValue = true)
    {
        var pointerElementType = (expression.Pointer.Type as PointerType)!.Type.ToLLVMType();
        var pointer = Process(expression.Pointer);

        var index = Process(expression.Index);

        return IndexPointer(pointerElementType, pointer, index, getValue);
    }




    public LLVMValueRef ProcessDefault(BoundDefaultExpression expression)
    {
        return Constant.GetDefaultValueForType(expression.Type!);
    }

    #endregion








    #region Generation Methods

    private LLVMValueRef CreateVariableAlloca(VariableSymbol symbol, string allocaName)
    {
        var llvmType = symbol.Type!.ToLLVMType();

        var reference = Builder.BuildAlloca(llvmType, allocaName);
        symbol.SetLLVMProperties(reference, llvmType, null);

        return reference;
    }




    private LLVMValueRef Cast(Type from, Type to, LLVMValueRef value)
    {
        var llvmFrom = from.ToLLVMType();
        var llvmTo = to.ToLLVMType();

        var sourceTypeSize = llvmFrom.SizeOfThisInMemory();
        var targetTypeSize = llvmTo.SizeOfThisInMemory();

        return CastOperation(from, to, value, llvmTo, sourceTypeSize, targetTypeSize);
    }


    private LLVMValueRef CastOperation(Type from, Type to, LLVMValueRef value, LLVMTypeRef llvmTo, int sourceTypeSize, int targetTypeSize) => (from, to) switch
    {
        _ when !from.IsPointer && to.IsPointer => Builder.BuildIntToPtr(value, llvmTo, "cast.int->ptr"), // int to pointer... is this really useful?
        _ when from.IsPointer && !to.IsPointer => Builder.BuildPtrToInt(value, llvmTo, "cast.ptr->int"), // pointer to int
        _ when from.IsPointer && to.IsPointer => Builder.BuildPointerCast(value, llvmTo, "cast.ptr"), // pointer to another pointer type

        _ when from.IsFloat && to.IsInteger && to.IsSigned => Builder.BuildFPToSI(value, llvmTo, "cast.float->int"), // float to int
        _ when from.IsFloat && to.IsInteger && to.IsUnsigned => Builder.BuildFPToUI(value, llvmTo, "cast.float->uint"), // float to uint
        _ when from.IsInteger && to.IsFloat && from.IsSigned => Builder.BuildSIToFP(value, llvmTo, "cast.int->float"), // int to float
        _ when from.IsInteger && to.IsFloat && from.IsUnsigned => Builder.BuildUIToFP(value, llvmTo, "cast.uint->float"), // uint to float
        _ when from.IsFloat && to.IsFloat => Builder.BuildFPCast(value, llvmTo, "cast.float"), // float to another float type

        _ when sourceTypeSize != targetTypeSize => Builder.BuildIntCast(value, llvmTo, "cast.int"), // integer to another integer type

        _ => value // source type is the same as target type
    };




    private LLVMValueRef Call(LLVMValueRef function, LLVMTypeRef functionType, IReadOnlyList<LLVMValueRef> arguments)
    {
        var returnValueName = functionType.ReturnType.Kind == LLVMTypeKind.LLVMVoidTypeKind ? "" : "return.value";
        return Builder.BuildCall2(functionType, function, arguments.ToArray(), returnValueName);
    }




    private LLVMValueRef IndexPointer(LLVMTypeRef addressElementType, LLVMValueRef address, LLVMValueRef index, bool getValue = true)
        => Index(addressElementType, address, index, getValue: getValue);

    private LLVMValueRef IndexArray(LLVMTypeRef addressElementType, LLVMValueRef address, LLVMValueRef index)
        => Index(addressElementType, address, index, false, false);

    // although this is a pointer, if you are indexing it, we suppose it's an array as well.
    private LLVMValueRef Index(LLVMTypeRef addressElementType, LLVMValueRef address, LLVMValueRef index, bool scalar = true, bool getValue = true)
    {
        var indices = scalar ? new[] { index } : new[] { Constant.Zero, index };
        var elementAddress = Builder.BuildGEP2(addressElementType, address, indices, "array.index.ptr");

        if (getValue)
            return Builder.BuildLoad2(addressElementType, elementAddress, "array.index.value");

        return elementAddress;
    }

    #endregion








    #region Debug Metadata

    private void DebugForLocationDo(Span? location, Action action)
    {
        DebugSetLocationTo(location);
        action();
        DebugSetLocationTo(null);
    }


    private void DebugSetLocationTo(Span? location)
    {
        if (location is null)
        {
            Debug?.SetCurrentLocation();
            return;
        }

        Debug?.SetCurrentLocation(location.Value.Line, location.Value.Start);
    }


    private LLVMMetadataRef? DebugCreateLocation(Span location)
        => Debug?.CreateDebugLocation(location.Line, location.Start);


    private LLVMMetadataRef? DebugCreateLexicalScope(Span location)
        => Debug?.CreateLexicalScope(location.Line, location.Start);




    private LLVMMetadataRef? DebugGenerateFunction(FunctionSymbol function)
        => Debug?.GenerateFunction(function);


    private LLVMMetadataRef? DebugGenerateLocalVariable(VariableSymbol variable)
        => Debug?.GenerateLocalVariable(variable);


    private LLVMMetadataRef? DebugGenerateParameter(VariableSymbol parameter, int index)
        => Debug?.GenerateParameter(parameter, index);




    // These methods are only necessary when the variable for some reason does not have an alloca (memory address).
    // if the variable has an alloca, the debugger is able to track its memory address and, consequently
    // its value. Since currently a variable cannot be created without alloca, they're useless
    private LLVMDbgRecordRef? DebugUpdateLocalVariableValue(string name, Span location)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);
        return Debug?.UpdateLocalVariableValue(name, llvmLocation!.Value);
    }


    private LLVMDbgRecordRef? DebugUpdateLocalVariableValue(LLVMValueRef reference, Span location)
    {
        var llvmLocation = Debug?.CreateDebugLocation(location.Line, location.Start);
        return Debug?.UpdateLocalVariableValue(reference, llvmLocation!.Value);
    }




    private void DebugGenerateScope(Scope scope, Span location, LLVMMetadataRef? debugFunctionReference = null)
    {
        var llvmDebugScopeMetadata = DebugCreateLexicalScope(location);
        llvmDebugScopeMetadata = debugFunctionReference ?? llvmDebugScopeMetadata;

        scope.DebugMetadata = llvmDebugScopeMetadata;
    }

    #endregion




    private static void TryCatchControlException(Action action)
    {
        try
        {
            action();
        }
        catch (CompilerControlException)
        {}
    }


    private static void UnreachableCode()
        => throw new UnreachableCodeControl();
}
