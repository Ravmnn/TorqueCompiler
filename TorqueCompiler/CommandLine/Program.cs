using System;

using Spectre.Console.Cli;

using Torque.CommandLine.Commands;


namespace Torque.CommandLine;




class Program
{
    const string TestFile = "general/main.tor";
    const string TestOptions = "-I imports -O build --debug";




    private static int Main(string[] args)
    {
        /* Environment.CurrentDirectory = "/home/ravmn/Documentos/programming/csharp/TorqueCompiler/examples/";
        args = $"compile {TestFile} {TestOptions}".Split(' '); */

        var root = new CommandApp();
        root.Configure(config =>
        {
            config.SetApplicationName("torque");
            config.SetApplicationVersion("dev");

            config.AddCommand<CompileCommand>("compile");
            config.AddCommand<LinkCommand>("link");
        });

        return root.Run(args);
    }




    private static void Test()
    {
        /* Torque.Initialize(new CompileCommandSettings {
            File = new FileInfo(TestFileFullPath), Debug = true,
            Architecture = Compiler.Target.ArchitectureType.X86_64,
            Environment = Compiler.Target.EnvironmentType.GNU,
            OperationalSystem = Compiler.Target.OperationalSystemType.Linux,
            Vendor = Compiler.Target.VendorType.PC
        });

        var statements = CompilerSteps.BuildFinalAST(SourceCode.Source!);
        var boundStatements = CompilerSteps.SemanticAnalysis(statements, SourceCode.FilePath!);
        var mainFunction = (from boundStatement in boundStatements.Statements
            let function = boundStatement as BoundFunctionDeclarationStatement
            where function is not null && function.Symbol.Name == "main"
            select function).First(); */
    }
}
