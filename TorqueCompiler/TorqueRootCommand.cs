using System.IO;
using System.CommandLine;
using System.CommandLine.Parsing;

using Torque.Compiler;


namespace Torque;




public class TorqueRootCommand : RootCommand
{
    public ParseResult Result { get; }


    public Argument<FileInfo> File { get; }

    public Option<string> Output { get; }

    public Option<int> BitMode { get; }
    public Option<string> EntryPoint { get; }




    public TorqueRootCommand(string[] args) : base("The official AOT compiler for the Torque programming language.")
    {
        File = new Argument<FileInfo>("file");
        File.AcceptExistingOnly();
        Add(File);


        Add(Output = new Option<string>("--output", "-o")
        {
            DefaultValueFactory = _ => "app"
        });


        Add(BitMode = new Option<int>("--bits")
        {
            DefaultValueFactory = _ => (int)Compiler.BitMode.Bits32,
            Validators = { BitsValidator }
        });

        Add(EntryPoint = new Option<string>("--entry-point")
        {
            DefaultValueFactory = _ => TorqueCompiler.DefaultEntryPoint
        });


        SetAction(Callback);

        Result = Parse(args);
    }


    private static void BitsValidator(OptionResult option)
    {
        if (option.GetValueOrDefault<int>() is
            not ((int)Compiler.BitMode.Bits16 or (int)Compiler.BitMode.Bits32 or (int)Compiler.BitMode.Bits64))
            option.AddError("Bits must be 16, 32 or 64");
    }




    private void Callback(ParseResult _)
        => Torque.Run(GetTorqueOptions());




    public TorqueOptions GetTorqueOptions() => new TorqueOptions
    {
        File = Result.GetRequiredValue(File),

        Output = Result.GetValue(Output)!,

        BitMode = (BitMode)Result.GetValue(BitMode),
        EntryPoint = Result.GetValue(EntryPoint)!
    };
}
