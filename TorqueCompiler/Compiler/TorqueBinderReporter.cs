using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Torque.CommandLine;
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
        ValidateStatementPlacement(statement);

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




    public void ProcessImport(ImportStatement statement)
    {
        var modulePath = statement.GetModulePath(Binder.ImportReference);

        if (!File.Exists(modulePath))
            Report(BinderCatalog.UnknownModule, location: statement.Location);

        CheckForMultipleDeclarationsAfterImport(statement.Location);
    }


    private void CheckForMultipleDeclarationsAfterImport(Span importLocation)
    {
        var reportedSymbols = new List<string>();
        var reportedTypes = new List<string>();

        foreach (var symbol in Binder.Scope.Symbols)
            if (!reportedSymbols.Contains(symbol.Name) && ReportIfImportedSymbolHasMultipleDeclarations(symbol.Syntax, importLocation))
                reportedSymbols.Add(symbol.Name);

        foreach (var type in Binder.DeclaredTypes.Types)
            if (!reportedTypes.Contains(type.TypeSymbol.Name) && ReportIfImportedSymbolHasMultipleDeclarations(type.TypeSymbol, importLocation))
                reportedTypes.Add(type.TypeSymbol.Name);
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
        if (expression.Expression is not SymbolExpression and not IndexingExpression and not MemberAccessExpression)
            Report(BinderCatalog.ValueMustBeAddressable, location: expression.Location);
    }




    public void ProcessAssignment(AssignmentExpression expression)
    {
        if (expression.Reference is not SymbolExpression and not PointerAccessExpression and not IndexingExpression and not MemberAccessExpression)
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
        if (ReportIfUnknownType(new BaseTypeSyntax(expression.Symbol)))
            return;

        ReportIfDeclaredTypeSyntaxIsNotOfKind<StructTypeSyntax>(expression.Symbol);
    }




    public void ProcessMemberAccess(MemberAccessExpression expression)
    { }




    private bool ReportIfMultipleDeclaration(SymbolSyntax symbol)
    {
        var (hasMultipleDeclarations, firstDeclaredAsType) = SymbolIsMultiDeclared(symbol);

        if (hasMultipleDeclarations && !firstDeclaredAsType)
        {
            ReportSymbol(BinderCatalog.MultipleSymbolDeclaration, symbol);
            return true;
        }

        if (firstDeclaredAsType)
        {
            ReportSymbol(BinderCatalog.SymbolAlreadyDeclaredAsType, symbol);
            return true;
        }

        return false;
    }


    private bool ReportIfImportedSymbolHasMultipleDeclarations(SymbolSyntax symbol, Span location)
    {
        var (hasMultipleDeclarations, _) = SymbolIsMultiDeclared(symbol);

        if (hasMultipleDeclarations)
        {
            Report(BinderCatalog.ImportedSymbolHasMultipleDeclarations, [symbol.Name], location);
            return true;
        }

        return false;
    }


    private (bool hasMultipleDeclarations, bool firstDeclaredAsType) SymbolIsMultiDeclared(SymbolSyntax symbol)
    {
        if (Binder.Scope.SymbolIsMultiDeclared(symbol.Name) || Binder.DeclaredTypes.TypeIsMultiDeclared(symbol.Name))
            return (true, false);

        if (Binder.Scope.SymbolExists(symbol.Name) && Binder.DeclaredTypes.IsDeclared(symbol.Name))
            return (true, true);

        return (false, false);
    }




    private bool ValidateStatementPlacement(Statement statement)
    {
        if (Binder.IsInFunctionScope && !statement.CanBeInFunctionScope)
        {
            Report(BinderCatalog.ThisStatementMustBePlacedAtFileScope, location: statement.Location);
            return true;
        }

        if (Binder.IsInFileScope && !statement.CanBeInFileScope)
        {
            Report(BinderCatalog.ThisStatementMustBePlacedAtFunctionScope, location: statement.Location);
            return true;
        }

        return false;
    }




    private void ValidateDeclarationModifiers(IDeclaration declaration)
    {
        ReportIfHasDuplicateModifiers(declaration, declaration.Symbol.Location);

        foreach (var modifier in declaration.Modifiers)
            ReportIfInvalidModifierTarget(declaration, modifier);
    }


    private bool ReportIfHasDuplicateModifiers(IModificable modificable, Span location)
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




    private bool ReportIfUnknownType(TypeSyntax type) => type switch
    {
        StructTypeSyntax structType => ReportIfUnknownForStructType(structType),
        FunctionTypeSyntax functionType => ReportIfUnknownForFunctionType(functionType),
        PointerTypeSyntax pointerType => ReportIfUnknownType(pointerType.InnerType),

        BaseTypeSyntax baseType => ReportIfUnknownForBaseType(baseType),

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

        if (Binder.DeclaredTypes.IsDeclared(symbol.Name))
            return false;

        Report(BinderCatalog.UnknownType, [symbol.Name], symbol.Location);
        return true;
    }




    private bool ReportIfDeclaredTypeSyntaxIsNotOfKind<T>(SymbolSyntax typeSymbol) where T : TypeSyntax
    {
        if (Binder.DeclaredTypes.IsTypeDeclarationSyntaxOfType<T>(typeSymbol.Name))
            return false;

        Report(BinderCatalog.InvalidTypeKind, location: typeSymbol.Location);
        return true;
    }
}
