using System.Diagnostics;
using System.Linq;

using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;
using Torque.Compiler.Types;
using Torque.Compiler.AST.Expressions;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;


namespace Torque.Compiler;




public sealed class TorqueBinderReporter(TorqueBinder binder) : DiagnosticReporter<BinderCatalog>,
    IExpressionProcessor, IStatementProcessor,
    IDeclarationProcessor
{
    public TorqueBinder Binder { get; } = binder;




    public void Process(IDeclaration declaration)
    {
        ReportIfMultipleDeclaration(declaration.Symbol);
        ValidateDeclarationModifiers(declaration);

        declaration.ProcessDeclaration(this);
    }




    public void ProcessFunctionDeclaration(FunctionDeclarationStatement declaration)
    { }




    public void ProcessAliasDeclaration(AliasDeclarationStatement declaration)
    { }




    public void ProcessStructDeclaration(StructDeclarationStatement declaration)
    {
        var structType = new StructTypeDeclaration(declaration.Symbol, declaration.Members);
        ReportIfUnknownType(structType.GetTypeSyntax());
    }








    public void Process(Statement statement)
    {
        ReportIfNonDeclarationAtFileScope(statement);

        if (statement is IDeclaration declaration)
            ReportIfWrongDeclarationPlacement(declaration);

        statement.Process(this);
    }




    public void ProcessExpression(ExpressionStatement statement)
    { }




    public void ProcessVariableDefinition(VariableDeclarationStatement statement)
    {
        if (!statement.InferType)
            ReportIfUnknownType(statement.Type);

        ReportIfMultipleDeclaration(statement.Name);
    }




    public void ProcessFunctionDefinition(FunctionDeclarationStatement statement)
    {
        ReportIfUnknownType(statement.ReturnType);

        foreach (var parameter in statement.Parameters)
            ReportIfUnknownType(parameter.Type);

        ValidateFunctionBody(statement);
    }


    private void ValidateFunctionBody(FunctionDeclarationStatement statement)
    {
        var isExternal = statement.IsExternal;
        var hasBody = statement.Body is not null;

        if (isExternal && hasBody)
            Report(BinderCatalog.ExternalFunctionCannotHaveABody, location: statement.Location);

        else if (!isExternal && !hasBody)
            Report(BinderCatalog.FunctionMustHaveABody, location: statement.Location);
    }




    public void ProcessReturn(ReturnStatement statement)
    { }




    public void ProcessBlock(BlockStatement statement)
    { }




    public void ProcessIf(IfStatement statement)
    { }




    public void ProcessWhile(WhileStatement statement)
    { }




    public void ProcessContinue(ContinueStatement statement)
    {
        ReportIfNotInsideALoop(statement);
    }




    public void ProcessBreak(BreakStatement statement)
    {
        ReportIfNotInsideALoop(statement);
    }








    public void Process(Expression expression)
        => expression.Process(this);




    public void ProcessLiteral(LiteralExpression expression)
    { }




    public void ProcessBinary(BinaryExpression expression)
    { }




    public void ProcessUnary(UnaryExpression expression)
    { }




    public void ProcessGrouping(GroupingExpression expression)
    { }




    public void ProcessComparison(ComparisonExpression expression)
    { }




    public void ProcessEquality(EqualityExpression expression)
    { }




    public void ProcessLogic(LogicExpression expression)
    { }




    public void ProcessSymbol(SymbolExpression expression)
    {
        var symbol = Binder.Scope.TryGetSymbol(expression.Symbol.Name);

        if (symbol is null)
            ReportSymbol(BinderCatalog.UndeclaredSymbol, expression.Symbol);

        else if (symbol is not VariableSymbol)
            ReportSymbol(BinderCatalog.SymbolIsNotAValue, expression.Symbol);
    }




    public void ProcessAddress(AddressExpression expression)
    {
        if (expression.Expression is not SymbolExpression and not IndexingExpression)
            Report(BinderCatalog.ValueMustBeAddressable, location: expression.Location);
    }




    public void ProcessAssignment(AssignmentExpression expression)
    {
        if (expression.Target is not SymbolExpression and not PointerAccessExpression and not IndexingExpression)
            Report(BinderCatalog.MustBeAssignmentReference, location: expression.Location);
    }




    public void ProcessPointerAccess(PointerAccessExpression expression)
    { }




    public void ProcessCall(CallExpression expression)
    { }




    public void ProcessCast(CastExpression expression)
    {
        ReportIfUnknownType(expression.Type);
    }




    public void ProcessArray(ArrayExpression expression)
    {
        ReportIfUnknownType(expression.ElementType);
    }




    public void ProcessIndexing(IndexingExpression expression)
    { }




    public void ProcessDefault(DefaultExpression expression)
    {
        ReportIfUnknownType(expression.TypeSyntax);
    }




    public void ProcessStruct(StructExpression expression)
    {
        ReportIfDeclaredTypeIsNotOfKind<StructTypeDeclaration>(expression.Symbol);
    }








    private bool ReportIfMultipleDeclaration(SymbolSyntax symbol)
    {
        if (!Binder.Scope.SymbolIsMultiDeclared(symbol.Name))
            return false;

        ReportSymbol(BinderCatalog.MultipleSymbolDeclaration, symbol);
        return true;
    }




    private bool ReportIfNonDeclarationAtFileScope(Statement statement)
    {
        if (Binder.IsInFunctionScope || statement is IDeclaration)
            return false;

        Report(BinderCatalog.OnlyDeclarationsCanExistInFileScope, location: statement.Location);
        return true;
    }




    private void ValidateDeclarationModifiers(IDeclaration declaration)
    {
        ReportIfHasDuplicateModifers(declaration, declaration.Symbol.Location);

        foreach (var modifier in declaration.Modifiers)
            ReportIfInvalidModifierTarget(declaration, modifier);
    }


    private bool ReportIfHasDuplicateModifers(IModificable modificable, Span location)
    {
        if (modificable.Modifiers.DistinctBy(modifier => modifier.Type).Count() == modificable.Modifiers.Count)
            return false;

        Report(BinderCatalog.MultipleSameModifiers, location: location);
        return true;
    }


    private bool ReportIfInvalidModifierTarget(IModificable modificable, Modifier modifier)
    {
        var modifierTargets = ModifiersTarget.GetFor(modifier);

        if (modifierTargets.Any(target => target.HasFlag(modificable.ThisTargetIdentity)))
            return false;

        Report(BinderCatalog.InvalidModifierTarget, location: modifier.Location);
        return true;
    }




    private bool ReportIfNotInsideALoop(Statement statement)
    {
        if (Binder.IsInsideALoop)
            return false;

        Report(BinderCatalog.LoopControlInstructionMustBeInLoop, location: statement.Location);
        return true;
    }




    private void ReportIfWrongDeclarationPlacement(IDeclaration declaration)
    {
        if ((Binder.IsInFileScope && !declaration.CanBeInFileScope) || (Binder.IsInFunctionScope && !declaration.CanBeInFunctionScope))
            Report(BinderCatalog.WrongDeclarationPlacement, location: declaration.Symbol.Location);
    }




    private bool ReportIfUnknownType(TypeSyntax type) => type switch
    {
        BaseTypeSyntax baseType => ReportIfUnknownForBaseType(baseType),

        FunctionTypeSyntax functionType => ReportIfUnknownForFunctionType(functionType),
        PointerTypeSyntax pointerType => ReportIfUnknownType(pointerType.InnerType),
        StructTypeSyntax structType => ReportIfUnknownForStructType(structType),

        _ => throw new UnreachableException()
    };


    private bool ReportIfUnknownForStructType(StructTypeSyntax structType)
    {
        foreach (var member in structType.Members)
            if (ReportIfUnknownType(member.Type))
                return true;

        return false;
    }


    private bool ReportIfUnknownForFunctionType(FunctionTypeSyntax functionType)
    {
        if (ReportIfUnknownType(functionType.ReturnType))
            return true;

        foreach (var parameterType in functionType.ParametersType)
            if (ReportIfUnknownType(parameterType))
                return true;

        return false;
    }


    private bool ReportIfUnknownForBaseType(BaseTypeSyntax type)
    {
        if (type.IsPrimitiveType)
            return false;

        var symbol = type.TypeSymbol;

        if (Binder.DeclaredTypes.IsDeclared(symbol))
            return false;

        Report(BinderCatalog.UnknownType, [symbol.Name], symbol.Location);
        return true;
    }




    private bool ReportIfDeclaredTypeIsNotOfKind<T>(SymbolSyntax typeSymbol) where T : TypeDeclaration
    {
        if (Binder.DeclaredTypes.IsDeclared<T>(typeSymbol))
            return false;

        Report(BinderCatalog.InvalidTypeKind, location: typeSymbol.Location);
        return true;
    }
}
