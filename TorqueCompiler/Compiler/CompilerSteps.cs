using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.CodeGen;
using Torque.Compiler.Diagnostics;
using Torque.Compiler.Parsing;
using Torque.Compiler.Semantic;
using Torque.Compiler.Semantic.CFA;
using Torque.Compiler.Tokens;


namespace Torque.Compiler;




public static class CompilerSteps
{
    public static Module SemanticAnalysis(IReadOnlyList<Statement> statements, SourceCode sourceCode, IModuleProvider moduleProvider)
    {
        var moduleContext = Bind(statements, sourceCode, moduleProvider);

        TypeCheck(moduleContext, sourceCode);
        AnalyzeControlFlow(moduleContext.Statements, sourceCode);

        return moduleContext;
    }


    public static IReadOnlyList<Statement> BuildFinalAST(SourceCode sourceCode)
    {
        var tokens = Tokenize(sourceCode);
        var statements = Parse(tokens, sourceCode);
        statements = Desugarize(statements);

        return statements;
    }




    public static string Compile(Module module, IRGenerationOptions options, FileSystem fileSystem)
    {
        var compiler = new IRGenerator(module, options, fileSystem);
        var llvmModule = compiler.GenerateModule();

        return llvmModule.PrintToString();
    }


    public static void AnalyzeControlFlow(IReadOnlyList<BoundStatement> boundStatements, SourceCode sourceCode)
    {
        var functionDeclarations = boundStatements.Cast<BoundFunctionDeclarationStatement>().ToArray();
        var graphs = ControlFlowGraphBuilder.BuildFromFunctionDeclarations(functionDeclarations);
        ControlFlowAnalysis.ExecuteAllAnalysis(graphs);

        var reporter = new ControlFlowGraphReporter(sourceCode);
        reporter.ReportAll(graphs);

        DiagnosticLogger.LogDiagnosticsAndInterruptIfAny(reporter.Diagnostics);
    }


    public static void TypeCheck(Module module, SourceCode sourceCode)
    {
        var typeChecker = new TypeChecker(module.Statements, module.DeclaredTypes, sourceCode);
        typeChecker.Check();

        DiagnosticLogger.LogDiagnosticsAndInterruptIfAny(typeChecker.Reporter.Diagnostics);
    }


    public static Module Bind(IReadOnlyList<Statement> statements, SourceCode sourceCode, IModuleProvider moduleProvider)
    {
        var binder = new Binder(statements, sourceCode, moduleProvider);
        var module = binder.Bind();

        DiagnosticLogger.LogDiagnosticsAndInterruptIfAny(binder.Reporter.Diagnostics);

        return module;
    }


    public static IReadOnlyList<Statement> Desugarize(IReadOnlyList<Statement> statements)
    {
        var desugarizer = new Desugarizer(statements);
        statements = desugarizer.Desugarize();

        // desugarizer cannot report diagnostics

        return statements;
    }


    public static IReadOnlyList<Statement> Parse(IReadOnlyList<Token> tokens, SourceCode sourceCode)
    {
        var parser = new Parser(tokens, sourceCode);
        var statements = parser.Parse();

        DiagnosticLogger.LogDiagnosticsAndInterruptIfAny(parser.Reporter.Diagnostics);

        return statements;
    }


    public static IReadOnlyList<Token> Tokenize(SourceCode sourceCode)
    {
        var lexer = new Lexer(sourceCode);
        var tokens = lexer.Tokenize();

        DiagnosticLogger.LogDiagnosticsAndInterruptIfAny(lexer.Reporter.Diagnostics);

        return tokens;
    }
}
