using System.IO;
using System.CommandLine;


namespace Torque;




public class CompileCommand : Command
{
    public Argument<FileInfo> File { get; }

    public Option<string> Output { get; }

    public Option<OutputType> OutputType { get; }

    public Option<bool> PrintAST { get; }
    public Option<bool> PrintLLVM { get; }





    public CompileCommand() : base("compile", "Compiles a source file into LLVM, Assembly or Object format.")
    {
        File = new Argument<FileInfo>("file");
        File.AcceptExistingOnly();
        Add(File);

        Add(Output = new Option<string>("--output", "-o"));

        Add(OutputType = new Option<OutputType>("--output-type", "-O")
        {
            DefaultValueFactory = _ => global::Torque.OutputType.Object
        });

        Add(PrintAST = new Option<bool>("--print-ast"));
        Add(PrintLLVM = new Option<bool>("--print-llvm"));


        SetAction(Callback);
    }


    private void Callback(ParseResult result)
        => Torque.Compile(GetOptions(result));




    public TorqueCompileOptions GetOptions(ParseResult result) => new TorqueCompileOptions
    {
        File = result.GetRequiredValue(File),

        Output = result.GetValue(Output),

        OutputType = result.GetValue(OutputType),

        PrintAST = result.GetValue(PrintAST),
        PrintLLVM = result.GetValue(PrintLLVM)
    };
}
