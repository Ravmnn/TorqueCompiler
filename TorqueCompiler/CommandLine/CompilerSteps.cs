using System.Collections.Generic;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.CodeGen;
using Torque.Compiler.Parsing;
using Torque.Compiler.Semantic;
using Torque.Compiler.Semantic.CFA;
using Torque.Compiler.Tokens;


namespace Torque.CommandLine;




public static class CompilerSteps
{
    public static Module SemanticAnalysis(IReadOnlyList<Statement> statements, string modulePath)
    {
        var moduleContext = Bind(statements, modulePath);

        TypeCheck(moduleContext);
        AnalyzeControlFlow(moduleContext.Statements);

        return moduleContext;
    }


    public static IReadOnlyList<Statement> BuildFinalAST(string source)
    {
        var tokens = Tokenize(source);
        var statements = Parse(tokens);
        statements = Desugarize(statements);

        return statements;
    }




    public static string Compile(Module module, IRGenerationOptions options)
    {
        var compiler = new IRGenerator(module, options);
        var llvmModule = compiler.GenerateModule();

        return llvmModule.PrintToString();
    }


    public static void AnalyzeControlFlow(IReadOnlyList<BoundStatement> boundStatements)
    {
        var functionDeclarations = boundStatements.Cast<BoundFunctionDeclarationStatement>().ToArray();
        var graphs = ControlFlowGraphBuilder.BuildFromFunctionDeclarations(functionDeclarations);
        ControlFlowAnalysis.ExecuteAllAnalysis(graphs);

        var reporter = new ControlFlowGraphReporter();
        reporter.ReportAll(graphs);

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(reporter.Diagnostics);
    }


    public static void TypeCheck(Module module)
    {
        var typeChecker = new TypeChecker(module.Statements, module.DeclaredTypes);
        typeChecker.Check();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(typeChecker.Reporter.Diagnostics);
    }


    public static Module Bind(IReadOnlyList<Statement> statements, string modulePath)
    {
        var binder = new Binder(statements, modulePath);
        var module = binder.Bind();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(binder.Reporter.Diagnostics);

        return module;
    }


    public static IReadOnlyList<Statement> Desugarize(IReadOnlyList<Statement> statements)
    {
        var desugarizer = new Desugarizer(statements);
        statements = desugarizer.Desugarize();

        // desugarizer cannot report diagnostics

        return statements;
    }


    public static IReadOnlyList<Statement> Parse(IReadOnlyList<Token> tokens)
    {
        var parser = new Parser(tokens);
        var statements = parser.Parse();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(parser.Reporter.Diagnostics);

        return statements;
    }


    public static IReadOnlyList<Token> Tokenize(string source)
    {
        var lexer = new Lexer(source);
        var tokens = lexer.Tokenize();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(lexer.Reporter.Diagnostics);

        return tokens;
    }
}
