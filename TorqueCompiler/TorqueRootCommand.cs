using System.IO;
using System.CommandLine;


namespace Torque;




public class TorqueRootCommand : RootCommand
{
    public ParseResult Result { get; }


    public Argument<FileInfo> File { get; }

    public Option<string> Output { get; }

    public Option<bool> PrintAST { get; }
    public Option<bool> PrintLLVM { get; }




    public TorqueRootCommand(string[] args) : base("The official AOT compiler for the Torque programming language.")
    {
        File = new Argument<FileInfo>("file");
        File.AcceptExistingOnly();
        Add(File);


        Add(Output = new Option<string>("--output", "-o")
        {
            DefaultValueFactory = _ => "output"
        });


        Add(PrintAST = new Option<bool>("--print-ast"));
        Add(PrintLLVM = new Option<bool>("--print-llvm"));


        SetAction(Callback);

        Result = Parse(args);
    }




    private void Callback(ParseResult _)
        => Torque.Run(GetTorqueOptions());




    public TorqueOptions GetTorqueOptions() => new TorqueOptions
    {
        File = Result.GetRequiredValue(File),

        Output = Result.GetValue(Output)!,

        PrintAST = Result.GetValue(PrintAST),
        PrintLLVM = Result.GetValue(PrintLLVM)
    };
}
