using Spectre.Console.Cli;


namespace Torque.CommandLine;




class Program
{
    private static int Main(string[] args)
    {
        //args = "compile /home/marvin/Documentos/program/csharp/TorqueCompiler/examples/test.tor --debug".Split(' ');

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
}
