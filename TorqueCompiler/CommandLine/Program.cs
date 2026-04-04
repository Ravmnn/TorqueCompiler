using System.IO;
using System.Linq;

using Spectre.Console.Cli;

using Torque.CommandLine.Commands;
using Torque.Compiler.BoundAST.Statements;


namespace Torque.CommandLine;




class Program
{
    const string TestFile = "general/main.tor";
    const string TestFileFullPath = $"/home/ravmn/Documentos/programming/csharp/TorqueCompiler/examples/{TestFile}";
    const string TestOptions = "-O /home/ravmn/Documentos/programming/csharp/TorqueCompiler/examples/build --debug";




    private static int Main(string[] args)
    {
        //args = $"compile {TestFileFullPath} {TestOptions}".Split(' ');

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
        Torque.Initialize(new CompileCommandSettings {
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
            select function).First();
    }
}
