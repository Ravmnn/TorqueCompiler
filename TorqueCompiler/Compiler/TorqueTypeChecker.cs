using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


using Type = Torque.Compiler.Types.Type;


namespace Torque.Compiler;




public class TorqueTypeChecker(IReadOnlyList<BoundStatement> statements, DeclaredTypeManager declaredTypes)
    : DiagnosticReporter<TypeCheckerCatalog>,
    IBoundStatementProcessor, IBoundExpressionProcessor<Type>,
    IBoundDeclarationProcessor
{
    public const PrimitiveType DefaultLiteralIntegerType = PrimitiveType.Int32;
    public const PrimitiveType DefaultLiteralFloatType = PrimitiveType.Float32;




    private bool _acceptVoidExpressions;

    private Type? _expectedReturnType;


    private DeclaredTypeManager DeclaredTypes { get; } = declaredTypes;


    public IReadOnlyList<BoundStatement> Statements { get; } = statements;




    public void Check()
    {
        Reset();

        CheckAllDeclarations();

        foreach (var statement in Statements)
            Process(statement);
    }


    private void Reset()
    {
        Diagnostics.Clear();
    }


    private void CheckAllDeclarations()
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
        var returnType = TypeFromTypeName(declaration.Syntax.ReturnType);
        var parametersType = ParametersTypeFromParametersDeclaration(declaration.Syntax.Parameters);

        SetFunctionAndParametersSymbolsType(declaration.FunctionSymbol, returnType, parametersType);
    }


    private void SetFunctionAndParametersSymbolsType(FunctionSymbol symbol, Type returnType, IReadOnlyList<Type> parametersType)
    {
        symbol.Type = new FunctionType(returnType, parametersType);
        SetFunctionSymbolParametersTypeIfNotExternal(symbol, parametersType);
    }


    private IReadOnlyList<Type> ParametersTypeFromParametersDeclaration(IReadOnlyList<GenericDeclaration> parameters)
        => parameters.Select(parameter => TypeFromNonVoidTypeName(parameter.Type)).ToArray();


    private void SetFunctionSymbolParametersTypeIfNotExternal(FunctionSymbol symbol, IReadOnlyList<Type> parametersType)
    {
        if (symbol.IsExternal)
            return;

        for (var i = 0; i < parametersType.Count; i++)
            symbol.Parameters[i].Type = parametersType[i];
    }

    #endregion




    #region Statements

    public void Process(BoundStatement statement)
        => statement.Process(this);




    public void ProcessExpression(BoundExpressionStatement statement)
    {
        _acceptVoidExpressions = true;
        Process(statement.Expression);
        _acceptVoidExpressions = false;
    }


    public void ProcessVariable(BoundVariableDeclarationStatement statement)
    {
        var typeSyntax = statement.Syntax.Type;

        // the use of "let" is only allowed for function-scope variables
        var valueType = Process(statement.Value);
        var symbolType = typeSyntax.IsAuto ? valueType : TypeFromNonVoidTypeName(typeSyntax);

        statement.VariableSymbol.Type = symbolType;
        statement.Value = ImplicitCastOrReport(symbolType, statement.Value, true);
    }




    public void ProcessFunction(BoundFunctionDeclarationStatement statement)
    {
        if (statement.IsExternal)
            return;

        _expectedReturnType = statement.FunctionSymbol.Type!.ReturnType;
        Process(statement.Body!);
        _expectedReturnType = null;
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        if (statement.Expression is null)
        {
            ReportIfExpectedTypeIsNotVoidAndDoesNotReturn(statement.Location);
            return;
        }

        ProcessReturnValue(statement);
    }


    private void ProcessReturnValue(BoundReturnStatement statement)
    {
        Process(statement.Expression!);

        if (!_expectedReturnType!.IsVoid)
            statement.Expression = ImplicitCastOrReport(_expectedReturnType, statement.Expression!);
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }




    public void ProcessIf(BoundIfStatement statement)
    {
        Process(statement.Condition);
        statement.Condition = ImplicitCastOrReport(PrimitiveType.Bool, statement.Condition);

        Process(statement.ThenStatement);

        if (statement.ElseStatement is not null)
            Process(statement.ElseStatement);
    }




    public void ProcessWhile(BoundWhileStatement statement)
    {
        Process(statement.Condition);
        statement.Condition = ImplicitCastOrReport(PrimitiveType.Bool, statement.Condition);

        Process(statement.Loop);

        if (statement.PostLoop is not null)
            Process(statement.PostLoop);
    }


    public void ProcessBreak(BoundBreakStatement statement)
    { }


    public void ProcessContinue(BoundContinueStatement statement)
    { }

    #endregion








    #region Expression

    public Type Process(BoundExpression expression)
    {
        var type = expression.Process(this);
        ReportIfVoidExpression(type, expression.Location);

        return type;
    }


    public void ProcessAll(IReadOnlyList<BoundExpression> expressions)
    {
        foreach (var expression in expressions)
            Process(expression);
    }




    public Type ProcessLiteral(BoundLiteralExpression expression)
    {
        expression.Value = expression.Syntax.Value;
        expression.Type = TypeOfLiteralObject(expression.Value);

        return expression.Type;
    }


    private Type TypeOfLiteralObject(object literal) => literal switch
    {
        IReadOnlyList<byte> => StringLiteralType(),

        bool => PrimitiveType.Bool,
        byte => PrimitiveType.Char,
        double => DefaultLiteralFloatType,
        ulong => DefaultLiteralIntegerType,

        _ => throw new UnreachableException()
    };


    private static PointerType StringLiteralType()
        => new PointerType(PrimitiveType.Char);




    public Type ProcessBinary(BoundBinaryExpression expression)
    {
        var leftType = Process(expression.Left);
        Process(expression.Right);

        expression.Right = ImplicitCastOrReport(leftType, expression.Right);

        return expression.Type!;
    }




    public Type ProcessUnary(BoundUnaryExpression expression)
    {
        Process(expression.Expression);

        switch (expression.Syntax.Operator)
        {
            case TokenType.Minus: break;
            case TokenType.Exclamation:
                expression.Expression = ImplicitCastOrReport(PrimitiveType.Bool, expression.Expression);
                break;

            default: throw new UnreachableException();
        }

        return expression.Type!;
    }




    public Type ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public Type ProcessComparison(BoundComparisonExpression expression)
    {
        // TODO: when compound types are supported, check that the expression here is a primitive (compounds cannot be compared, same for equality)

        var leftType = Process(expression.Left);
        Process(expression.Right);

        expression.Right = ImplicitCastOrReport(leftType, expression.Right);
        return expression.Type;
    }


    public Type ProcessEquality(BoundEqualityExpression expression)
    {
        var leftType = Process(expression.Left);
        Process(expression.Right);

        expression.Right = ImplicitCastOrReport(leftType, expression.Right);

        return expression.Type;
    }


    public Type ProcessLogic(BoundLogicExpression expression)
    {
        Process(expression.Left);
        Process(expression.Right);

        expression.Left = ImplicitCastOrReport(PrimitiveType.Bool, expression.Left);
        expression.Right = ImplicitCastOrReport(PrimitiveType.Bool, expression.Right);

        return expression.Type;
    }




    public Type ProcessSymbol(BoundSymbolExpression expression)
        => expression.Type;




    public Type ProcessAddress(BoundAddressExpression expression)
    {
        Process(expression.Expression);
        return expression.Type;
    }


    public Type ProcessAddressable(BoundAddressableExpression expression)
    {
        Process(expression.Expression);
        return expression.Type!;
    }




    public Type ProcessAssignment(BoundAssignmentExpression expression)
    {
        var referenceType = Process(expression.Reference);
        Process(expression.Value);

        expression.Value = ImplicitCastOrReport(referenceType, expression.Value);

        return expression.Type!;
    }


    public Type ProcessAssignmentReference(BoundAssignmentReferenceExpression expression)
        => Process(expression.Reference);




    public Type ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        var type = Process(expression.Pointer);
        ReportIfNotAPointer(type, expression.Pointer.Location);

        return expression.Type!;
    }




    public Type ProcessCall(BoundCallExpression expression)
    {
        Process(expression.Callee);
        ProcessAll(expression.Arguments.ToArray());

        CheckCallExpression(expression);

        // if the callee has not a function type, "expression.Type" will be null.
        // fall backing this has no problem because this class reports diagnostics
        // if that's the case

        return expression.Type ?? Type.Void;
    }


    private void CheckCallExpression(BoundCallExpression expression)
    {
        if (expression.Callee.Type is not FunctionType functionType)
        {
            Report(TypeCheckerCatalog.CannotCallNonFunction, location: expression.Location);
            return;
        }

        ReportIfArityDiffers(expression);
        MatchArgumentsTypeWithFunctionType(expression.Arguments, functionType);
    }


    private void MatchArgumentsTypeWithFunctionType(IList<BoundExpression> arguments, FunctionType functionType)
    {
        var parametersType = functionType.ParametersType;

        for (var i = 0; i < parametersType.Count && i < arguments.Count; i++)
            arguments[i] = ImplicitCastOrReport(parametersType[i], arguments[i]);
    }




    public Type ProcessCast(BoundCastExpression expression)
    {
        Process(expression.Value);
        expression.Type = TypeFromNonVoidTypeName(expression.Syntax.Type);

        return expression.Type!;
    }




    public Type ProcessImplicitCast(BoundImplicitCastExpression expression)
        => throw new UnreachableException();




    public Type ProcessArray(BoundArrayExpression expression)
    {
        var elementType = TypeFromTypeName(expression.Syntax.ElementType);

        expression.ArrayType = new ArrayType(elementType, expression.Syntax.Length); // this is the type used to the alloca
        expression.Type = new PointerType(elementType); // to avoid any future hidden bug, force the use of the pointer type

        CheckArrayExpression(expression, elementType);

        return expression.Type;
    }


    private void CheckArrayExpression(BoundArrayExpression expression, Type elementType)
    {
        if (expression.Syntax.Length == 0)
            Report(TypeCheckerCatalog.CannotHaveAZeroSizedArray, location: expression.Location);

        if (expression.Elements is not null)
            MatchElementTypes(expression.Elements, elementType);
    }


    private void MatchElementTypes(IList<BoundExpression> elements, Type elementType)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];

            Process(element);
            elements[i] = ImplicitCastOrReport(elementType, element);
        }
    }




    public Type ProcessIndexing(BoundIndexingExpression expression)
    {
        Process(expression.Pointer);
        Process(expression.Index);

        ReportIfNotAPointer(expression.Pointer.Type!, expression.Pointer.Location);
        expression.Index = ImplicitCastOrReport(PrimitiveType.Int64, expression.Index);

        return expression.Type!;
    }




    public Type ProcessDefault(BoundDefaultExpression expression)
    {
        expression.Type = TypeFromNonVoidTypeName(expression.Syntax.TypeSyntax);
        return expression.Type;
    }




    public Type ProcessStruct(BoundStructExpression expression)
    {
        var structType = DeclaredTypes.TryGetType<StructTypeDeclaration>(expression.Syntax.Symbol)!;

        for (var index = 0; index < expression.InitializationList.Count; index++)
        {
            var initialization = expression.InitializationList[index];
            var declaration = structType.Members.First(member => member.Name.Name == initialization.Member.Name);

            ProcessStructMemberInitialization(initialization, declaration);
        }

        expression.Type = TypeFromTypeName(structType.GetTypeSyntax());

        return expression.Type!;
    }


    private void ProcessStructMemberInitialization(BoundStructMemberInitialization initialization, GenericDeclaration declaration)
    {
        Process(initialization.Value);

        var expectedType = TypeFromTypeName(declaration.Type);
        initialization.Value = ImplicitCastOrReport(expectedType, initialization.Value, true);
    }

    #endregion








    #region Diagnostic Reporting

    private BoundExpression ImplicitCastOrReport(Type expected, BoundExpression expression, bool forceIfLiteral = false)
    {
        if (TryImplicitCast(expected, expression, forceIfLiteral) is { } result)
            return result;

        Report(TypeCheckerCatalog.TypeDiffers, [expected.ToString(), expression.Type!.ToString()], expression.Location);
        return expression;
    }


    private BoundExpression? TryImplicitCast(Type expected, BoundExpression expression, bool forceIfLiteral = false)
    {
        // here, "expression" should already have been processed (typed)

        var got = expression.Type!;

        if (expected == got)
            return expression;

        var forceForBaseTypes = expression is BoundLiteralExpression && forceIfLiteral;

        if (TryImplicitCast(got, expected, forceForBaseTypes) is not null || GenericPointerToPointer(expected, expression))
            return new BoundImplicitCastExpression(expression, expected);

        return null;
    }


    private static bool GenericPointerToPointer(Type expected, BoundExpression expression)
        => expected.IsPointer && expression.Type!.IsGenericPointer;


    private bool ReportIfNotAPointer(Type type, Span location)
    {
        if (type.IsPointer)
            return false;

        Report(TypeCheckerCatalog.PointerExpected, location: location);
        return true;
    }


    private bool ReportIfVoidTypeName(Type type, Span location)
    {
        // Here, although "void" should be reported, it is important to check whether
        // the type is a function type or not, since function types may return void, and
        // that's alright.

        if (!type.IsVoid || type is FunctionType)
            return false;

        Report(TypeCheckerCatalog.CannotUseVoidHere, location: location);
        return true;
    }


    private bool ReportIfVoidExpression(Type type, Span location)
    {
        if (_acceptVoidExpressions || !type.IsVoid || type is FunctionType)
            return false;

        Report(TypeCheckerCatalog.ExpressionDoesNotReturnAnyValue, location: location);
        return true;
    }


    private bool ReportIfArityDiffers(BoundCallExpression expression)
    {
        var functionType = (expression.Callee.Type as FunctionType)!;
        return ReportIfArityDiffers(functionType.ParametersType.Count, expression.Arguments.Count, expression.Location);
    }


    private bool ReportIfArityDiffers(int expected, int got, Span location)
    {
        if (expected == got)
            return false;

        Report(TypeCheckerCatalog.ArityDiffers, [expected, got], location);
        return true;
    }


    private bool ReportIfExpectedTypeIsNotVoidAndDoesNotReturn(Span location)
    {
        if (_expectedReturnType!.IsVoid)
            return false;

        Report(TypeCheckerCatalog.ExpectedAReturnValue, location: location);
        return true;
    }

    #endregion




    #region Implicit Casting
    // TODO: move all of this to a single class?

    private Type? TryImplicitCast(Type from, Type to, bool forceForBaseTypes = false)
    {
        if (!CanImplicitCast(from, to, forceForBaseTypes))
            return null;

        return to;
    }


    private bool CanImplicitCast(Type from, Type to, bool forceForBaseTypes = false)
    {
        var anyIsCompound = from.IsCompound || to.IsCompound;

        if (anyIsCompound)
            return false;

        var sameTypes = from == to;
        var bothBase = from.IsBase && to.IsBase;
        var anyIsAuto = from.IsAuto || to.IsAuto;

        if (sameTypes || anyIsAuto)
            return true;

        if (bothBase && forceForBaseTypes)
            return true;

        if (!bothBase)
            return false;

        var signDiffers = from.IsSigned != to.IsSigned;
        var floatToInt = from.IsFloat && to.IsInteger; // float to int may result in loss of data
        var sourceBigger = from.SizeOfTypeInMemory() > to.SizeOfTypeInMemory();

        if (signDiffers || floatToInt || sourceBigger)
            return false;

        return true;
    }

    #endregion




    #region Type Convertors
    // TODO: move all of this to a single class?

    private Type TypeFromNonVoidTypeName(TypeSyntax typeSyntax)
    {
        var type = TypeFromTypeName(typeSyntax);
        ReportIfVoidTypeName(type, typeSyntax.BaseType.TypeSymbol.Location);

        return type;
    }




    private Type TypeFromTypeName(TypeSyntax typeSyntax) => typeSyntax switch
    {
        StructTypeSyntax structTypeSyntax => StructTypeFromTypeName(structTypeSyntax),

        FunctionTypeSyntax functionTypeName => FunctionTypeFromTypeName(functionTypeName),
        PointerTypeSyntax pointerTypeName => TypeFromPointerTypeName(pointerTypeName),

        BaseTypeSyntax baseTypeName => TypeFromBaseTypeName(baseTypeName),

        _ => throw new UnreachableException()
    };


    private StructType StructTypeFromTypeName(StructTypeSyntax structTypeSyntax)
    {
        var boundMembers = new List<BoundGenericDeclaration>();

        foreach (var member in structTypeSyntax.Members)
            boundMembers.Add(new BoundGenericDeclaration(TypeFromTypeName(member.Type), member.Name));

        return new StructType(boundMembers);
    }


    private FunctionType FunctionTypeFromTypeName(FunctionTypeSyntax typeSyntax)
    {
        var parametersType = typeSyntax.ParametersType.Select(TypeFromTypeName).ToArray();
        var returnType = TypeFromTypeName(typeSyntax.ReturnType);

        return new FunctionType(returnType, parametersType);
    }


    private PointerType TypeFromPointerTypeName(PointerTypeSyntax pointerTypeSyntax)
        => new PointerType(TypeFromTypeName(pointerTypeSyntax.InnerType));


    private Type TypeFromBaseTypeName(BaseTypeSyntax typeSyntax)
    {
        if (typeSyntax.IsAuto)
            Report(TypeCheckerCatalog.CannotUseLetHere, location: typeSyntax.TypeSymbol.Location);

        if (typeSyntax.IsPrimitiveType)
            return new BasePrimitiveType(typeSyntax.BaseType.TypeSymbol.SymbolToPrimitiveType());

        return TypeFromTypeDeclaration(typeSyntax);
    }


    private Type TypeFromTypeDeclaration(BaseTypeSyntax typeSyntax)
    {
        var typeDeclaration = DeclaredTypes.TryGetType(typeSyntax.TypeSymbol)!;
        var declarationTypeSyntax = typeDeclaration.GetTypeSyntax();
        return TypeFromTypeName(declarationTypeSyntax);
    }

    #endregion
}
