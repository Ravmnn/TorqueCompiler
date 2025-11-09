using System.IO;
using System.CommandLine;


namespace TorqueCompiler;




public class TorqueRootCommand : RootCommand
{
    public ParseResult Result { get; }


    public Argument<FileInfo> File { get; }




    public TorqueRootCommand(string[] args) : base("The official AOT compiler for the Torque programming language.")
    {
        Add(File = new Argument<FileInfo>("file"));

        SetAction(Callback);

        Result = Parse(args);
        Result.Invoke();
    }


    private void Callback(ParseResult _)
    {
        TorqueOptions.Global = GetTorqueOptions();
        Torque.Run();
    }




    public TorqueOptions GetTorqueOptions() => new TorqueOptions
    {
        File = Result.GetRequiredValue(File)
    };
}
