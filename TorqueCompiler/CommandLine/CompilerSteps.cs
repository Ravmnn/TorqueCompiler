using System.Collections.Generic;
using System.Linq;

using Torque.Compiler;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Tokens;
using Torque.Compiler.Types;


namespace Torque.CommandLine;




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


    public static void TypeCheck(IReadOnlyList<BoundStatement> boundStatements, IReadOnlyList<TypeDeclaration> declaredTypes)
    {
        var typeChecker = new TorqueTypeChecker(boundStatements, declaredTypes);
        typeChecker.Check();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(typeChecker.Diagnostics);
    }


    public static (IReadOnlyList<BoundStatement>, Scope, IReadOnlyList<TypeDeclaration>) Bind(IReadOnlyList<Statement> statements)
    {
        var binder = new TorqueBinder(statements);
        var boundStatements = binder.Bind();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(binder.Diagnostics);

        return (boundStatements, binder.Scope, binder.DeclaredTypes);
    }


    public static IReadOnlyList<Statement> Desugarize(IReadOnlyList<Statement> statements)
    {
        var desugarizer = new TorqueDesugarizer(statements);
        statements = desugarizer.Desugarize(); // desugarizer cannot fail

        return statements;
    }


    public static IReadOnlyList<Statement> Parse(IReadOnlyList<Token> tokens)
    {
        var parser = new TorqueParser(tokens);
        var statements = parser.Parse();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(parser.Diagnostics);

        return statements;
    }


    public static IReadOnlyList<Token> Tokenize()
    {
        var lexer = new TorqueLexer(SourceCode.Source!);
        var tokens = lexer.Tokenize();

        Torque.Logger.LogDiagnosticsAndInterruptIfAny(lexer.Diagnostics);

        return tokens;
    }
}
