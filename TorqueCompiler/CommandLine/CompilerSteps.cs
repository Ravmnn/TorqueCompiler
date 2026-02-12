using System.Collections.Generic;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Tokens;


namespace Torque.CommandLine;




public readonly record struct ModuleContext(IReadOnlyList<BoundStatement> Statements, Scope Scope,
    DeclaredTypeManager DeclaredTypes);




public static class CompilerSteps
{
    public static string Compile(IReadOnlyList<BoundStatement> boundStatements, Scope scope, CompileCommandSettings settings)
    {
        var compiler = new TorqueCompiler(boundStatements, scope, settings.File, settings.Debug);
        var bitCode = compiler.Compile();

        return bitCode;
    }


    public static void AnalyzeControlFlow(IReadOnlyList<BoundStatement> boundStatements)
    {
        var functionDeclarations = boundStatements.Cast<BoundFunctionDeclarationStatement>().ToArray();
        var graphs = new ControlFlowGraphBuilder(functionDeclarations).Build();

        var controlFlowReporter = new ControlFlowAnalysisReporter(graphs);
        controlFlowReporter.Report();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(controlFlowReporter.Diagnostics);
    }


    public static void TypeCheck(ModuleContext moduleContext)
    {
        var typeChecker = new TorqueTypeChecker(moduleContext.Statements, moduleContext.DeclaredTypes);
        typeChecker.Check();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(typeChecker.Reporter.Diagnostics);
    }


    public static ModuleContext Bind(IReadOnlyList<Statement> statements)
    {
        var binder = new TorqueBinder(statements);
        var boundStatements = binder.Bind();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(binder.Reporter.Diagnostics);

        return new ModuleContext(boundStatements, binder.Scope, binder.DeclaredTypes);
    }


    public static IReadOnlyList<Statement> Desugarize(IReadOnlyList<Statement> statements)
    {
        var desugarizer = new TorqueDesugarizer(statements);
        statements = desugarizer.Desugarize();

        // desugarizer cannot have report diagnostics

        return statements;
    }


    public static IReadOnlyList<Statement> Parse(IReadOnlyList<Token> tokens)
    {
        var parser = new TorqueParser(tokens);
        var statements = parser.Parse();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(parser.Reporter.Diagnostics);

        return statements;
    }


    public static IReadOnlyList<Token> Tokenize(string source)
    {
        var lexer = new TorqueLexer(source);
        var tokens = lexer.Tokenize();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(lexer.Reporter.Diagnostics);

        return tokens;
    }
}
