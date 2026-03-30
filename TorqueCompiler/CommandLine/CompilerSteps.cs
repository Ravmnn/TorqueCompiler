using System.Collections.Generic;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Tokens;


namespace Torque.CommandLine;




public static class CompilerSteps
{
    public static string Compile(Module module, CompilerOptions options)
    {
        var compiler = new TorqueCompiler(module, options);
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


    public static void TypeCheck(Module module)
    {
        var typeChecker = new TorqueTypeChecker(module.Statements, module.DeclaredTypes);

        try
        {
            typeChecker.Check();
        }
        finally
        {
            Torque.Logger.LogDiagnosticsAndInterruptIfAny(typeChecker.Reporter.Diagnostics);
        }
    }


    public static Module Bind(IReadOnlyList<Statement> statements, string importReference, string modulePath)
    {
        var binder = new TorqueBinder(statements, modulePath);
        var module = binder.Bind();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(binder.Reporter.Diagnostics);

        return module;
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
