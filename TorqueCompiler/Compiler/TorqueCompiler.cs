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
using Torque.Compiler.Target;


using Type = Torque.Compiler.Types.Type;


namespace Torque.Compiler;




public class TorqueCompiler : IBoundStatementProcessor, IBoundExpressionProcessor<ExpressionResult>, IBoundDeclarationProcessor
{

    // if debug metadata generation is desired, this property must be set, since
    // debug metadata uses some file information
    public FileInfo? File { get; }


    private LLVMModuleRef _llvmModule = LLVMModuleRef.CreateWithName("MainModule");
    public LLVMModuleRef LLVMModule => _llvmModule;

    public LLVMBuilderRef Builder { get; } = LLVMBuilderRef.Create(LLVMContextRef.Global);

    public TargetMachine TargetMachine { get; }
    public LLVMTargetDataRef DataLayout => TargetMachine.DataLayout;

    public DebugMetadataGenerator? Debug { get; }


    private LLVMValueRef? _currentFunction;
    private LLVMBasicBlockRef? _currentFunctionEntry;
    private LLVMBasicBlockRef? _currentFunctionBody;

    private readonly Stack<LLVMBasicBlockRef> _loopConditionBlockStack = new Stack<LLVMBasicBlockRef>();
    private readonly Stack<LLVMBasicBlockRef> _loopJoinBlockStack = new Stack<LLVMBasicBlockRef>();
    private readonly Stack<LLVMBasicBlockRef> _loopPostLoopBlockStack = new Stack<LLVMBasicBlockRef>();

    private readonly IntrinsicCaller _intrinsics;


    public Module Module { get; }

    public IReadOnlyList<BoundStatement> Statements => Module.Statements;

    public Scope GlobalScope => Module.Scope;

    private Scope _scope = null!;
    public Scope Scope
    {
        get => _scope;
        private init => _scope = value;
    }

    public TypeBuilder TypeBuilder { get; }




    public TorqueCompiler(Module module, FileInfo? file = null, bool generateDebugMetadata = false)
    {
        // TODO: add optimization command line options (later... this is more useful after this language is able to do more stuff)
        // TODO: add support to generic code

        // TODO: add operator overloading, custom implicit and explicit cast support as well
        // TODO: add interfaces
        // TODO: structs should have methods
        // TODO: variadic functions?

        // TODO: add sizeof(T)
        // TODO: add pre-processing support
        // TODO: add importing system
        // TODO: add enums
        // TODO: default values for parameters and struct fields
        // TODO: add number suffixes, for binary, hexadecimal, uints, floats...
        // TODO: "for" should accept "let" at the initializer
        // TODO: add expr += ... (and others)
        // TODO: add expr++ and expr**

        // TODO: check for circular imports (infinite)

        // TODO: check for already defined symbols when importing

        // TODO: add options that control the importing system to the command line
        // TODO: CFA is not working properly


        File = file;

        TargetMachine = TargetMachine.Global ?? throw new InvalidOperationException("The global target machine instance must be initialized");
        _llvmModule.Target = TargetMachine.Triple;
        _llvmModule.DataLayout = TargetMachine.StringDataLayout;

        _intrinsics = new IntrinsicCaller(LLVMModule, Builder);

        Module = module;
        Scope = GlobalScope;

        TypeBuilder = new TypeBuilder();

        if (generateDebugMetadata)
            Debug = new DebugMetadataGenerator(this);
    }


    public TorqueCompiler(TorqueCompiler compiler, Module module)
    {
        File = compiler.File;

        _llvmModule = compiler._llvmModule;
        _intrinsics = compiler._intrinsics;
        Builder = compiler.Builder;
        Debug = compiler.Debug;
        TypeBuilder = compiler.TypeBuilder;
        TargetMachine = compiler.TargetMachine;

        Module = module;
        Scope = GlobalScope;

        if (compiler.Debug is not null)
            Debug = new DebugMetadataGenerator(compiler.Debug, this);
    }




    public string Compile()
    {
        // Many parts of the code in this file assumes that there are values in some nullables
        // as "BoundExpression.Type", and that "BoundExpression.Syntax" (or "BoundStatement.Syntax")
        // stores the correct value according to the real expression (or statement).
        // Because of that, no null check is performed and if any system exception is thrown
        // due to null access or bad type conversion, or it is a bug, or the caller of the compiler API
        // didn't set up (binding, type checking...) the statements correctly.

        CompileImportedModules();

        DeclareAllDeclarations();

        foreach (var statement in Statements)
            Process(statement);

        Debug?.FinalizeGenerator();

        return LLVMModule.PrintToString();
    }


    private void CompileImportedModules()
    {
        foreach (var module in Module.ImportedModules)
            CompileImportedModule(module);
    }


    private void CompileImportedModule(Module module)
    {
        var compiler = new TorqueCompiler(this, module);
        compiler.Compile();
    }


    private void DeclareAllDeclarations()
    {
        foreach (var statement in Statements)
            if (statement is IBoundDeclaration declaration)
                Process(declaration);
    }








    #region Declarations

    public void Process(IBoundDeclaration declaration)
        => declaration.ProcessDeclaration(this);




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement declaration)
    {
        var functionSymbol = declaration.FunctionSymbol;

        var llvmFunctionType = TypeBuilder.ProcessRawFunction(functionSymbol.Type!);
        var llvmReference = LLVMModule.AddFunction(functionSymbol.Name, llvmFunctionType);
        llvmReference.Linkage = LLVMLinkage.LLVMExternalLinkage;

        functionSymbol.SetLLVMProperties(llvmReference, llvmFunctionType, null);
        functionSymbol.LLVMDebugMetadata = DebugGenerateFunction(functionSymbol);
    }

    #endregion








    #region Statements

    public void Process(BoundStatement statement)
    {
        statement.Process(this);
    }




    public void ProcessExpression(BoundExpressionStatement statement)
    {
        DebugForSetLocationDo(statement.Location, () => Process(statement.Expression));
    }




    public void ProcessVariable(BoundVariableDeclarationStatement statement)
    {
        var symbol = statement.VariableSymbol;
        var reference = CreateVariableAlloca(symbol, $"var.${symbol.Name}");
        symbol.LLVMDebugMetadata = DebugGenerateLocalVariable(symbol);

        DebugForSetLocationDo(statement.Location, () =>
        {
            var value = EnsureValue(Process(statement.Value)).Value;
            Builder.BuildStore(value, reference);
        });
    }




    public void ProcessFunction(BoundFunctionDeclarationStatement statement)
    {
        if (statement.Body is null)
            return;

        var functionSymbol = statement.FunctionSymbol;
        var reference = functionSymbol.LLVMReference!.Value;
        var entry = reference.AppendBasicBlock("entry");
        var body = reference.AppendBasicBlock("body");

        Builder.PositionAtEnd(entry);
        Builder.BuildBr(body);
        Builder.PositionAtEnd(body);

        _currentFunction = reference;
        _currentFunctionEntry = entry;
        _currentFunctionBody = body;
        ProcessFunctionBlock(statement.Body, functionSymbol);
        _currentFunction = null;
        _currentFunctionEntry = null;
        _currentFunctionBody = null;
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        if (statement.Expression is not null)
            DebugForSetLocationDo(statement.Location, () =>
                Builder.BuildRet(EnsureValue(Process(statement.Expression)).Value));
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
        var thenBlock = LLVM.CreateBasicBlockInContext(LLVMModule.Context, "then".StringToSBytePtr());
        var elseBlock = LLVM.CreateBasicBlockInContext(LLVMModule.Context, "else".StringToSBytePtr());
        var joinBlock = LLVM.CreateBasicBlockInContext(LLVMModule.Context, "join".StringToSBytePtr());

        var hasElse = statement.ElseStatement is not null;

        var condition = EnsureValue(Process(statement.Condition)).Value;

        DebugForSetLocationDo(statement.Location, () =>
        {
            Builder.BuildCondBr(condition, thenBlock, hasElse ? elseBlock : joinBlock);
            ProcessBasicBlock(statement.ThenStatement, thenBlock, joinBlock);

            if (hasElse)
                ProcessBasicBlock(statement.ElseStatement!, elseBlock, joinBlock);
        });

        AppendBlockAndPositionAtEnd(joinBlock);
    }




    public unsafe void ProcessWhile(BoundWhileStatement statement)
    {
        var conditionBlock = LLVM.CreateBasicBlockInContext(LLVMModule.Context, "condition".StringToSBytePtr());
        var loopBlock = LLVM.CreateBasicBlockInContext(LLVMModule.Context, "loop".StringToSBytePtr());
        var postLoopBlock = LLVM.CreateBasicBlockInContext(LLVMModule.Context, "postLoop".StringToSBytePtr());
        var joinBlock = LLVM.CreateBasicBlockInContext(LLVMModule.Context, "join".StringToSBytePtr());

        ProcessWhileConditionBlock(statement, conditionBlock, loopBlock, joinBlock);
        ProcessWhileBodyBlock(statement, conditionBlock, loopBlock, postLoopBlock, joinBlock);

        AppendBlockAndPositionAtEnd(joinBlock);
    }


    private unsafe void ProcessWhileBodyBlock(BoundWhileStatement statement, LLVMOpaqueBasicBlock* conditionBlock,
        LLVMOpaqueBasicBlock* loopBlock, LLVMOpaqueBasicBlock* postLoopBlock, LLVMOpaqueBasicBlock* joinBlock)
    {
        _loopConditionBlockStack.Push(conditionBlock);
        _loopPostLoopBlockStack.Push(postLoopBlock);
        _loopJoinBlockStack.Push(joinBlock);

        DebugForSetLocationDo(statement.Location, () =>
            ProcessBasicBlock(statement.Loop, loopBlock, postLoopBlock));

        ProcessBasicBlock(statement.PostLoop, postLoopBlock, conditionBlock);

        _loopConditionBlockStack.Pop();
        _loopPostLoopBlockStack.Pop();
        _loopJoinBlockStack.Pop();
    }


    private unsafe void ProcessWhileConditionBlock(BoundWhileStatement statement, LLVMOpaqueBasicBlock* conditionBlock,
        LLVMOpaqueBasicBlock* loopBlock, LLVMOpaqueBasicBlock* joinBlock)
    {
        Builder.BuildBr(conditionBlock);

        DebugSetLocationTo(statement.Condition.Location);

        AppendBlockAndPositionAtEnd(conditionBlock);
        var condition = EnsureValue(Process(statement.Condition)).Value;
        Builder.BuildCondBr(condition, loopBlock, joinBlock);
    }




    public void ProcessBreak(BoundBreakStatement statement)
    {
        DebugForSetLocationDo(statement.Location, () =>
            Builder.BuildBr(_loopJoinBlockStack.Peek()));
    }


    public void ProcessContinue(BoundContinueStatement statement)
    {
        DebugForSetLocationDo(statement.Location, () =>
            Builder.BuildBr(_loopPostLoopBlockStack.Peek()));
    }

    #endregion








    #region Expressions

    public ExpressionResult Process(BoundExpression expression)
        => expression.Process(this);


    public IReadOnlyList<ExpressionResult> ProcessAll(IReadOnlyList<BoundExpression> expressions)
        => expressions.Select(Process).ToArray();




    public ExpressionResult ProcessLiteral(BoundLiteralExpression expression)
    {
        var type = expression.Type!;
        var llvmType = TypeBuilder.Process(type);
        var value = expression.Value!;

        var literalValue = ValueFromLiteral(type, value, llvmType);
        return Value(literalValue, llvmType);
    }


    private LLVMValueRef ValueFromLiteral(Type type, object value, LLVMTypeRef llvmType) => type.BasePrimitive.Type switch
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




    public ExpressionResult ProcessBinary(BoundBinaryExpression expression)
    {
        var left = EnsureValue(Process(expression.Left)).Value;
        var right = EnsureValue(Process(expression.Right)).Value;

        var leftType = expression.Left.Type!;
        var llvmLeftType = TypeBuilder.Process(leftType);

        return Value(ProcessBinaryOperation(expression, leftType, left, right), llvmLeftType);
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




    public ExpressionResult ProcessUnary(BoundUnaryExpression expression)
    {
        var value = EnsureValue(Process(expression.Expression)).Value;
        var type = expression.Type!;
        var llvmType = TypeBuilder.Process(type);

        var operation = expression.Syntax.Operator;

        var operationResult = ProcessUnaryOperation(operation, type, llvmType, value);
        return Value(operationResult, llvmType);
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




    public ExpressionResult ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public ExpressionResult ProcessComparison(BoundComparisonExpression expression)
    {
        var left = EnsureValue(Process(expression.Left)).Value;
        var right = EnsureValue(Process(expression.Right)).Value;

        var leftType = expression.Left.Type!;
        var llvmLeftType = TypeBuilder.Process(leftType);

        var @operator = expression.Syntax.Operator;
        var isSigned = expression.Type.IsSigned;

        var operationResult = ProcessComparisonOperation(leftType, @operator, left, right, isSigned);
        return Value(operationResult, llvmLeftType);
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
            TokenType.LessThanOrEqual => !signed ? LLVMIntPredicate.LLVMIntULE : LLVMIntPredicate.LLVMIntSLE,

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




    public ExpressionResult ProcessEquality(BoundEqualityExpression expression)
    {
        var left = EnsureValue(Process(expression.Left)).Value;
        var right = EnsureValue(Process(expression.Right)).Value;

        var leftType = expression.Left.Type!;
        var llvmLeftType = TypeBuilder.Process(leftType);

        var operationValue = ProcessEqualityOperation(expression, leftType, left, right);
        return Value(operationValue, llvmLeftType);
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




    public ExpressionResult ProcessLogic(BoundLogicExpression expression)
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

        var llvmType = TypeBuilder.Process(expression.Type);
        return Value(BuildLogicOperationPhi(trueBlock, falseBlock), llvmType);
    }


    private void BuildLogicOperationOperandEvaluationBlocks(BoundLogicExpression expression, LLVMBasicBlockRef leftThenBlock,
        LLVMBasicBlockRef leftElseBlock, LLVMBasicBlockRef rhsBlock, LLVMBasicBlockRef trueBlock, LLVMBasicBlockRef falseBlock)
    {
        Builder.BuildCondBr(EnsureValue(Process(expression.Left)).Value, leftThenBlock, leftElseBlock);

        Builder.PositionAtEnd(rhsBlock);
        Builder.BuildCondBr(EnsureValue(Process(expression.Right)).Value, trueBlock, falseBlock);
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




    public ExpressionResult ProcessSymbol(BoundSymbolExpression expression)
    {
        var symbol = expression.Symbol;

        var llvmReference = symbol.LLVMReference!.Value;
        var llvmType = symbol.LLVMType!.Value;

        return Address(llvmReference, llvmType);
    }




    public ExpressionResult ProcessAddress(BoundAddressExpression expression)
    {
        var address =  EnsureAddress(Process(expression.Expression)).Value;
        var llvmType = TypeBuilder.Process(expression.Type);

        return Value(address, llvmType);
    }




    public ExpressionResult ProcessAssignment(BoundAssignmentExpression expression)
    {
        var reference = EnsureAddress(Process(expression.Reference)).Value;
        var value = EnsureValue(Process(expression.Value)).Value;

        Builder.BuildStore(value, reference);

        var llvmType = TypeBuilder.Process(expression.Type!);
        return Value(value, llvmType);
    }




    public ExpressionResult ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        var address = EnsureAddress(Process(expression.Pointer)).Value;
        var llvmPointerType = TypeBuilder.Process(expression.Pointer.Type!);
        var llvmType = TypeBuilder.Process(expression.Type!);

        var value = Builder.BuildLoad2(llvmPointerType, address, "access.ptr");
        return Address(value, llvmType);
    }




    public ExpressionResult ProcessCall(BoundCallExpression expression)
    {
        var function = EnsureAddress(Process(expression.Callee)).Value;
        var arguments = ProcessAll(expression.Arguments.AsReadOnly()).Select(EnsureValue);
        var llvmValueArguments = arguments.Select(argument => argument.Value).ToArray();

        var functionType = (expression.Callee.Type as FunctionType)!;
        var llvmFunctionType = TypeBuilder.ProcessRawFunction(functionType);

        var llvmReturnType = TypeBuilder.Process(functionType.ReturnType);
        LLVMValueRef? returnValue = null;

        DebugForSetLocationDo(expression.Location, () =>
        {
            returnValue = Call(function, llvmFunctionType, llvmValueArguments);
        });

        return Value(returnValue!.Value, llvmReturnType);
    }




    public ExpressionResult ProcessCast(BoundCastExpression expression)
    {
        var value = EnsureValue(Process(expression.Value)).Value;

        var from = expression.Value.Type!;
        var to = expression.Type!;
        var llvmToType = TypeBuilder.Process(to);

        var cast = Cast(from, to, value);
        return Value(cast, llvmToType);
    }




    public ExpressionResult ProcessImplicitCast(BoundImplicitCastExpression expression)
    {
        var value = EnsureValue(Process(expression.Value)).Value;

        var from = expression.Value.Type!;
        var to = expression.Type!;
        var llvmToType = TypeBuilder.Process(to);

        var cast = Cast(from, to, value);
        return Value(cast, llvmToType);
    }




    public ExpressionResult ProcessArray(BoundArrayExpression expression)
    {
        var arrayType = (expression.ArrayType as ArrayType)!;

        var llvmArrayType = TypeBuilder.Process(arrayType);
        var arrayAddress = CreateAlloca(llvmArrayType, "array.address");

        InitializeArrayElements(expression, llvmArrayType, arrayAddress);
        var firstElementAddress = IndexArray(llvmArrayType, arrayAddress, Constant.Zero, false);

        return Value(firstElementAddress, llvmArrayType);
    }


    private void InitializeArrayElements(BoundArrayExpression expression, LLVMTypeRef llvmArrayType, LLVMValueRef arrayAddress)
    {
        var initializationList = expression.Elements is not null ? ProcessAll(expression.Elements.ToArray()).Select(EnsureValue).ToArray() : [];
        var llvmValueInitializationList = initializationList.Select(item => item.Value).ToArray();

        InitializeArrayFromInitializationList(llvmArrayType, arrayAddress, llvmValueInitializationList);

        if ((ulong)llvmValueInitializationList.Length < expression.Syntax.Length)
            InitializeRemainingArrayElements((expression.ArrayType as ArrayType)!, llvmArrayType, arrayAddress, (ulong)llvmValueInitializationList.Length);
    }


    private void InitializeArrayFromInitializationList(LLVMTypeRef llvmArrayType, LLVMValueRef arrayAddress, LLVMValueRef[] llvmElements)
    {
        for (var i = 0; i < llvmElements.Length; i++)
        {
            var elementAddress = IndexArray(llvmArrayType, arrayAddress, Constant.Integer((ulong)i), false);
            Builder.BuildStore(llvmElements[i], elementAddress);
        }
    }


    private void InitializeRemainingArrayElements(ArrayType arrayType, LLVMTypeRef llvmArrayType, LLVMValueRef arrayAddress, ulong initializationListLength)
    {
        var elementTypeSize = (ulong)TypeBuilder.SizeOfTypeInMemory(arrayType.InnerType);
        var startAddress = IndexArray(llvmArrayType, arrayAddress, Constant.Integer(initializationListLength), false);

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




    public ExpressionResult ProcessIndexing(BoundIndexingExpression expression)
    {
        var pointerType = (expression.Pointer.Type as PointerType)!;
        var llvmPointerElementType = TypeBuilder.Process(pointerType.InnerType);

        var pointer = EnsureValue(Process(expression.Pointer)).Value;
        var index = EnsureValue(Process(expression.Index)).Value;

        var element = IndexPointer(llvmPointerElementType, pointer, index, false);
        return Address(element, llvmPointerElementType);
    }




    public ExpressionResult ProcessDefault(BoundDefaultExpression expression)
    {
        var llvmType = TypeBuilder.Process(expression.Type!);
        return Value(GetDefaultValueForType(expression.Type!), llvmType);
    }




    public ExpressionResult ProcessStruct(BoundStructExpression expression)
    {
        var structType = (expression.Type as StructType)!;
        var llvmStructType = TypeBuilder.Process(structType);
        var structValue = CreateStruct(structType, expression.InitializationList);

        return Value(structValue, llvmStructType);
    }


    public ExpressionResult ProcessMemberAccess(BoundMemberAccessExpression expression)
    {
        var structType = (expression.Compound.Type as StructType)!;
        var @struct = EnsureAddress(Process(expression.Compound)).Value;

        var memberName = expression.Member.Name;
        var memberType = structType.GetField(memberName)!.Value.member.Type;
        var llvmMemberType = TypeBuilder.Process(memberType);
        return Address(IndexStruct(@struct, structType, memberName, false), llvmMemberType);
    }

    #endregion








    #region Generation Methods

    private LLVMValueRef CreateVariableAlloca(VariableSymbol symbol, string allocaName)
    {
        var llvmType = TypeBuilder.Process(symbol.Type!);

        var reference = CreateAlloca(llvmType, allocaName);
        symbol.SetLLVMProperties(reference, llvmType, null);

        return reference;
    }


    private LLVMValueRef CreateAlloca(LLVMTypeRef type, string name)
    {
        var lastBlock = Builder.InsertBlock;

        PositionBuilderAtEndOfAllocaSection();

        var alloca = Builder.BuildAlloca(type, name);
        Builder.PositionAtEnd(lastBlock);

        return alloca;
    }


    private unsafe void PositionBuilderAtEndOfAllocaSection()
    {
        var last = LLVM.GetLastInstruction(_currentFunctionEntry!.Value);

        Builder.PositionBefore(last);
    }




    private LLVMValueRef Cast(Type from, Type to, LLVMValueRef value)
    {
        var llvmFrom = TypeBuilder.Process(from);
        var llvmTo = TypeBuilder.Process(to);

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




    private LLVMValueRef IndexStruct(LLVMValueRef structAddress, StructType structType, string memberName, bool getValue = true)
    {
        var llvmStructType = TypeBuilder.Process(structType);

        var (member, memberIndex) = structType.GetField(memberName)!.Value;
        var llvmMemberType = TypeBuilder.Process(member.Type);
        var memberAddress = Builder.BuildStructGEP2(llvmStructType, structAddress, (uint)memberIndex, $"member.${memberName}.address");

        // TODO: the use of getValue is now irrelevant
        if (getValue)
            return Builder.BuildLoad2(llvmMemberType, memberAddress, $"member.${memberName}");

        return memberAddress;
    }


    private LLVMValueRef IndexPointer(LLVMTypeRef addressElementType, LLVMValueRef address, LLVMValueRef index, bool getValue = true)
        => Index(addressElementType, address, index, getValue: getValue);


    private LLVMValueRef IndexArray(LLVMTypeRef addressElementType, LLVMValueRef address, LLVMValueRef index, bool getValue = true)
        => Index(addressElementType, address, index, false, getValue);


    private LLVMValueRef Index(LLVMTypeRef addressElementType, LLVMValueRef address, LLVMValueRef index, bool scalar = true, bool getValue = true)
    {
        var indices = scalar ? new[] { index } : new[] { Constant.Zero, index };
        var elementAddress = Builder.BuildGEP2(addressElementType, address, indices, "indexing.address");

        if (getValue)
            return Builder.BuildLoad2(addressElementType, elementAddress, "indexing.value");

        return elementAddress;
    }




    private unsafe void ProcessBasicBlock(BoundStatement? statement, LLVMOpaqueBasicBlock* block, LLVMOpaqueBasicBlock* join)
    {
        ProcessBasicBlock(statement, block);
        Builder.BuildBr(join);
    }


    private unsafe void ProcessBasicBlock(BoundStatement? statement, LLVMOpaqueBasicBlock* block)
    {
        AppendBlockAndPositionAtEnd(block);

        if (statement is null)
            return;

        DebugSetLocationTo(statement.Location);
        TryCatchControlException(() => Process(statement));
    }


    private unsafe void AppendBlockAndPositionAtEnd(LLVMOpaqueBasicBlock* block)
    {
        LLVM.AppendExistingBasicBlock(_currentFunction!.Value, block);
        Builder.PositionAtEnd(block);
    }




    public LLVMValueRef GetDefaultValueForType(Type type)
    {
        var typeBuilder = new TypeBuilder();
        var llvmType = typeBuilder.Process(type);

        return type switch
        {
            _ when type.IsStruct => GetDefaultValueForStruct((type as StructType)!),
            _ when type.IsPointer => Constant.NullPointer(),
            _ when type.IsFloat => Constant.Real(0, llvmType),

            _ => Constant.Integer(0, llvmType)
        };
    }


    private LLVMValueRef GetDefaultValueForStruct(StructType type)
        => CreateStruct(type);


    public LLVMValueRef CreateStruct(StructType type, IReadOnlyList<BoundStructMemberInitialization>? initializationList = null)
    {
        var values = GetDefaultValuesFromStruct(type);

        if (initializationList is not null)
            InsertInitializationList(type, initializationList, values);

        return CreateStruct(type, values);
    }


    private unsafe LLVMValueRef CreateStruct(StructType type, List<LLVMValueRef> values)
    {
        var llvmType = TypeBuilder.Process(type);
        var @struct = LLVM.GetUndef(llvmType);

        for (var i = 0; i < values.Count; i++)
            @struct = Builder.BuildInsertValue(@struct, values[i], (uint)i, $"field{i}");

        return @struct;
    }


    private List<LLVMValueRef> GetDefaultValuesFromStruct(StructType type)
    {
        var defaultValues = new List<LLVMValueRef>();

        for (var i = 0; i < type.Members.Count; i++)
        {
            var memberType = type.Members[i].Type;
            var memberValue = GetDefaultValueForType(memberType);

            defaultValues.Add(memberValue);
        }

        return defaultValues;
    }


    private void InsertInitializationList(StructType type, IReadOnlyList<BoundStructMemberInitialization> initializationList, List<LLVMValueRef> source)
    {
        for (var i = 0; i < initializationList.Count; i++)
        {
            var current = initializationList[i];
            var initializationIndex = type.GetField(current.Member.Name)!.Value.index;
            var value = EnsureValue(Process(current.Value)).Value;

            source[initializationIndex] = value;
        }
    }

    #endregion








    #region Debug Metadata

    private void DebugForSetLocationDo(Span? location, Action action)
    {
        DebugSetLocationTo(location);
        action();
        DebugSetLocationTo(null);
    }


    private T DebugForSetLocationDo<T>(Span? location, Func<T> func)
    {
        DebugSetLocationTo(location);
        var value = func();
        DebugSetLocationTo(null);

        return value;
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




    private void DebugGenerateScope(Scope scope, Span location, LLVMMetadataRef? debugFunctionReference = null)
    {
        var llvmDebugScopeMetadata = DebugCreateLexicalScope(location);
        llvmDebugScopeMetadata = debugFunctionReference ?? llvmDebugScopeMetadata;

        scope.DebugMetadata = llvmDebugScopeMetadata;
    }

    #endregion




    private ExpressionResult Address(LLVMValueRef address, LLVMTypeRef type)
        => new ExpressionResult(address, type, true);


    private ExpressionResult Value(LLVMValueRef value, LLVMTypeRef type)
        => new ExpressionResult(value, type, false);


    private ExpressionResult EnsureAddress(ExpressionResult expression)
    {
        if (expression.IsAddress)
            return expression;

        var temporaryAddress = CreateAlloca(expression.Type, "tmp.address");
        Builder.BuildStore(expression.Value, temporaryAddress);

        return Address(temporaryAddress, expression.Type);
    }


    private ExpressionResult EnsureValue(ExpressionResult expression)
    {
        if (expression.IsValue)
            return expression;

        var value = Builder.BuildLoad2(expression.Type, expression.Value, "value");

        return Value(value, expression.Type);
    }




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
