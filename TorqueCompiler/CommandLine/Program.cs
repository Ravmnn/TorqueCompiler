using Spectre.Console.Cli;


namespace Torque.CommandLine;




class Program
{
    private static int Main(string[] args)
    {
        /* const string TestFile = "general/main.tor";
        const string TestOptions = "--debug";
        args = $"compile /home/ravmn/Documentos/programming/csharp/TorqueCompiler/examples/{TestFile} {TestOptions}".Split(' ');
 */
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
