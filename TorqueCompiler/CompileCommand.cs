using System.IO;
using System.CommandLine;


namespace Torque;




public class CompileCommand : Command
{
    public Argument<FileInfo> File { get; }

    public Option<string> Output { get; }

    public Option<OutputType> OutputType { get; }

    public Option<bool> Debug { get; }

    public Option<bool> PrintAST { get; }
    public Option<bool> PrintLLVM { get; }
    public Option<bool> PrintASM { get; }





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

        Add(Debug = new Option<bool>("--debug"));

        Add(PrintAST = new Option<bool>("--print-ast"));
        Add(PrintLLVM = new Option<bool>("--print-llvm"));
        Add(PrintASM = new Option<bool>("--print-asm"));


        SetAction(Callback);
    }


    private void Callback(ParseResult result)
        => Torque.Compile(GetOptions(result));




    public TorqueCompileOptions GetOptions(ParseResult result) => new TorqueCompileOptions
    {
        File = result.GetRequiredValue(File),

        Output = result.GetValue(Output),

        OutputType = result.GetValue(OutputType),

        Debug = result.GetValue(Debug),

        PrintAST = result.GetValue(PrintAST),
        PrintLLVM = result.GetValue(PrintLLVM),
        PrintASM = result.GetValue(PrintASM)
    };
}
