using Spectre.Console.Cli;


namespace Torque.CommandLine;




class Program
{
    private static int Main(string[] args)
    {
        //args = "compile /home/marvin/Documentos/program/csharp/TorqueCompiler/examples/test.tor --print-llvm --debug".Split(' ');

        var root = new CommandApp();
        root.Configure(config =>
        {
            config.SetApplicationName("psyan");
            config.SetApplicationVersion("dev");

            config.AddCommand<CompileCommand>("compile");
            config.AddCommand<LinkCommand>("link");
        });

        return root.Run(args);
    }
}
