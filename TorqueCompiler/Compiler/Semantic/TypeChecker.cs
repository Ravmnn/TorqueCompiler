using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.BoundAST.Expressions;
using Torque.Compiler.BoundAST.Statements;


using Type = Torque.Compiler.Types.Type;


namespace Torque.Compiler.Semantic;




public class TypeChecker : IBoundStatementProcessor, IBoundExpressionProcessor<Type>, IBoundDeclarationProcessor
{
    internal class TypeCheckerDiagnosticReportedException : Exception;




    public const PrimitiveType DefaultLiteralIntegerType = PrimitiveType.Int32;
    public const PrimitiveType DefaultLiteralFloatType = PrimitiveType.Float32;




    public bool AcceptVoidExpressions { get; private set; }
    public Type? ExpectedReturnType { get; private set; }


    public DeclaredTypeManager DeclaredTypes { get; }
    public IReadOnlyList<BoundStatement> Statements { get; }

    public TypeCheckerReporter Reporter { get; private set; }
    public TypeSyntaxConverter Converter { get; private set; }




    public TypeChecker(IReadOnlyList<BoundStatement> statements, DeclaredTypeManager declaredTypes)
    {
        DeclaredTypes = declaredTypes;
        Statements = statements;

        Reporter = new TypeCheckerReporter(this);
        Converter = new TypeSyntaxConverter(this);
    }




    public void Check()
    {
        CheckAllTypeDeclarations();
        CheckAllDeclarations();

        foreach (var statement in Statements)
            Process(statement);
    }


    private void CheckAllTypeDeclarations()
    {
        foreach (var type in DeclaredTypes.Items)
            type.Type = Converter.TypeFromTypeSyntax(type.TypeSyntax);
    }


    private void CheckAllDeclarations()
    {
        foreach (var statement in Statements)
            if (statement is IBoundDeclaration declaration)
                Process(declaration);
    }




    #region Declarations

    public void Process(IBoundDeclaration declaration)
    {
        declaration.ProcessDeclaration(this);
        Reporter.Process(declaration);
    }




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement declaration)
    {
        var returnType = Converter.TypeFromTypeSyntax(declaration.Syntax.ReturnType);
        var parametersType = ParametersTypeFromParametersDeclaration(declaration.Syntax.Parameters);

        SetFunctionAndParametersSymbolsType(declaration.FunctionSymbol, returnType, parametersType);
    }


    private void SetFunctionAndParametersSymbolsType(FunctionSymbol symbol, Type returnType, IReadOnlyList<Type> parametersType)
    {
        symbol.Type = new FunctionType(returnType, parametersType);
        SetFunctionSymbolParametersTypeIfNotExternal(symbol, parametersType);
    }


    private IReadOnlyList<Type> ParametersTypeFromParametersDeclaration(IReadOnlyList<GenericDeclaration> parameters)
        => parameters.Select(parameter => Converter.TypeFromNonVoidTypeSyntax(parameter.Type)).ToArray();


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
    {
        statement.Process(this);
        Reporter.Process(statement);
    }




    public void ProcessExpression(BoundExpressionStatement statement)
    {
        AcceptVoidExpressions = true;
        Process(statement.Expression);
        AcceptVoidExpressions = false;
    }




    public void ProcessVariable(BoundVariableDeclarationStatement statement)
    {
        var typeSyntax = statement.Syntax.Type;

        var valueType = Process(statement.Value);
        var symbolType = statement.InferType ? valueType : Converter.TypeFromNonVoidTypeSyntax(typeSyntax);

        statement.VariableSymbol.Type = symbolType;
        statement.Value = MatchTypeOrImplicitCast(symbolType, statement.Value);
    }




    public void ProcessFunction(BoundFunctionDeclarationStatement statement)
    {
        if (statement.IsExternal)
            return;

        ExpectedReturnType = statement.FunctionSymbol.Type!.ReturnType;
        Process(statement.Body!);
        ExpectedReturnType = null;
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        if (statement.Expression is null)
            return;

        ProcessReturnValue(statement);
    }


    private void ProcessReturnValue(BoundReturnStatement statement)
    {
        Process(statement.Expression!);

        if (!ExpectedReturnType!.IsVoid)
            statement.Expression = MatchTypeOrImplicitCast(ExpectedReturnType, statement.Expression!);
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }




    public void ProcessIf(BoundIfStatement statement)
    {
        Process(statement.Condition);
        statement.Condition = MatchTypeOrImplicitCast(PrimitiveType.Bool, statement.Condition);

        Process(statement.ThenStatement);

        if (statement.ElseStatement is not null)
            Process(statement.ElseStatement);
    }




    public void ProcessWhile(BoundWhileStatement statement)
    {
        Process(statement.Condition);
        statement.Condition = MatchTypeOrImplicitCast(PrimitiveType.Bool, statement.Condition);

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
        Reporter.Process(expression);

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
        var rightType = Process(expression.Right);

        expression.Type = TypePromotion.Promote(leftType, rightType);
        ImplicitCastBinaryOperandToPromotedType(expression);

        return expression.Type;
    }




    public Type ProcessUnary(BoundUnaryExpression expression)
    {
        var innerExpression = expression.Expression;
        Process(innerExpression);

        switch (expression.Syntax.Operator)
        {
            case TokenType.Minus:
                expression.Type = ValidateTypeOrError(innerExpression.Type, type => type.IsNumber);
                break;

            case TokenType.Exclamation:
                expression.Expression = MatchTypeOrImplicitCast(PrimitiveType.Bool, innerExpression);
                expression.Type = PrimitiveType.Bool;
                break;

            default: throw new UnreachableException();
        }

        return expression.Type;
    }




    public Type ProcessGrouping(BoundGroupingExpression expression)
        => Process(expression.Expression);




    public Type ProcessComparison(BoundComparisonExpression expression)
    {
        var leftType = Process(expression.Left);
        var rightType = Process(expression.Right);

        var promotedType = TypePromotion.Promote(leftType, rightType, result => result.IsNumber);
        ImplicitCastBinaryOperandToPromotedType(expression, promotedType);

        expression.Type = PrimitiveType.Bool;

        return expression.Type;
    }


    public Type ProcessEquality(BoundEqualityExpression expression)
    {
        var leftType = Process(expression.Left);
        var rightType = Process(expression.Right);

        var promotedType = TypePromotion.Promote(leftType, rightType);
        ImplicitCastBinaryOperandToPromotedType(expression, promotedType);

        expression.Type = PrimitiveType.Bool;

        return expression.Type;
    }


    public Type ProcessLogic(BoundLogicExpression expression)
    {
        Process(expression.Left);
        Process(expression.Right);

        expression.Left = MatchTypeOrImplicitCast(PrimitiveType.Bool, expression.Left);
        expression.Right = MatchTypeOrImplicitCast(PrimitiveType.Bool, expression.Right);

        return expression.Type;
    }




    public Type ProcessSymbol(BoundSymbolExpression expression)
        => expression.Type;




    public Type ProcessAddress(BoundAddressExpression expression)
    {
        Process(expression.Expression);
        return expression.Type;
    }




    public Type ProcessAssignment(BoundAssignmentExpression expression)
    {
        var referenceType = Process(expression.Reference);
        Process(expression.Value);

        expression.Value = MatchTypeOrImplicitCast(referenceType, expression.Value);

        return expression.Type;
    }




    public Type ProcessPointerAccess(BoundPointerAccessExpression expression)
    {
        Process(expression.Pointer);
        return expression.Type;
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
            return;

        MatchArgumentsTypeWithFunctionType(expression.Arguments, functionType);
    }


    private void MatchArgumentsTypeWithFunctionType(IList<BoundExpression> arguments, FunctionType functionType)
    {
        var parametersType = functionType.ParametersType;

        for (var i = 0; i < parametersType.Count && i < arguments.Count; i++)
            arguments[i] = MatchTypeOrImplicitCast(parametersType[i], arguments[i]);
    }




    public Type ProcessCast(BoundCastExpression expression)
    {
        Process(expression.Value);
        expression.Type = Converter.TypeFromNonVoidTypeSyntax(expression.Syntax.Type);

        return expression.Type;
    }




    public Type ProcessImplicitCast(BoundImplicitCastExpression expression)
        => throw new UnreachableException();




    public Type ProcessArray(BoundArrayExpression expression)
    {
        var elementType = Converter.TypeFromNonVoidTypeSyntax(expression.Syntax.ElementType);

        expression.ArrayType = new ArrayType(elementType, expression.Syntax.Length); // this is the type used to the alloca
        expression.Type = new PointerType(elementType); // to avoid any future hidden bug, force the use of the pointer type

        CheckArrayExpression(expression, elementType);

        return expression.Type;
    }


    private void CheckArrayExpression(BoundArrayExpression expression, Type elementType)
    {
        if (expression.Elements is not null)
            MatchElementTypes(expression.Elements, elementType);
    }


    private void MatchElementTypes(IList<BoundExpression> elements, Type elementType)
    {
        for (var i = 0; i < elements.Count; i++)
        {
            var element = elements[i];

            Process(element);
            elements[i] = MatchTypeOrImplicitCast(elementType, element);
        }
    }




    public Type ProcessIndexing(BoundIndexingExpression expression)
    {
        Process(expression.Pointer);
        Process(expression.Index);

        expression.Index = MatchTypeOrImplicitCast(PrimitiveType.Int64, expression.Index);

        return expression.Type;
    }




    public Type ProcessDefault(BoundDefaultExpression expression)
    {
        expression.Type = Converter.TypeFromNonVoidTypeSyntax(expression.Syntax.TypeSyntax);
        return expression.Type;
    }




    public Type ProcessStruct(BoundStructExpression expression)
    {
        var structType = (DeclaredTypes.TryGet(expression.Syntax.Symbol.Name)!.TypeSyntax as StructTypeSyntax)!;

        for (var index = 0; index < expression.InitializationList.Count; index++)
        {
            var initialization = expression.InitializationList[index];
            var declaration = structType.Members.FirstOrDefault(member => member.Name.Name == initialization.Member.Name);

            if (declaration == default)
                break;

            ProcessStructMemberInitialization(initialization, declaration);
        }

        expression.Type = Converter.TypeFromNonVoidTypeSyntax(structType);

        return expression.Type!;
    }


    private void ProcessStructMemberInitialization(BoundStructMemberInitialization initialization, GenericDeclaration declaration)
    {
        Process(initialization.Value);

        var expectedType = Converter.TypeFromNonVoidTypeSyntax(declaration.Type);
        initialization.Value = MatchTypeOrImplicitCast(expectedType, initialization.Value);
    }




    public Type ProcessMemberAccess(BoundMemberAccessExpression expression)
    {
        if (Process(expression.Compound) is StructType structType)
            if (structType.GetField(expression.Member.Name) is var (field, _))
                expression.Type = field.Type;

        return expression.Type;
    }

    #endregion








    private BoundExpression MatchTypeOrImplicitCast(Type expected, BoundExpression expression)
    {
        if (TryMatchTypeOrImplicitCast(expected, expression) is { } result)
            return result;

        Reporter.ReportTypeDiffers(expected, expression.Type, expression.Location);
        return expression;
    }


    private BoundExpression? TryMatchTypeOrImplicitCast(Type expected, BoundExpression expression)
    {
        // here, "expression" should already have been processed (typed)

        var got = expression.Type;

        if (expected == got)
            return expression;

        var couldImplicitCast = TypeCaster.TryImplicitCast(got, expected) is not null;

        if (couldImplicitCast)
            return new BoundImplicitCastExpression(expression, expected);

        return null;
    }




    private static Type ValidateTypeOrError(Type type, Func<Type, bool> validator)
    {
        if (!validator(type))
            return Type.Error;

        return type;
    }




    private void ImplicitCastBinaryOperandToPromotedType(IBoundBinaryLayoutExpression binary)
        => ImplicitCastBinaryOperandToPromotedType(binary, binary.Type);


    private void ImplicitCastBinaryOperandToPromotedType(IBoundBinaryLayoutExpression binary, Type type)
    {
        if (!binary.Type.IsValid)
            return;

        if (type == binary.Left.Type)
            binary.Right = MatchTypeOrImplicitCast(type, binary.Right);
        else
            binary.Left = MatchTypeOrImplicitCast(type, binary.Left);
    }
}
