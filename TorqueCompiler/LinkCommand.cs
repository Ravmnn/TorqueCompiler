using System.IO;
using System.Collections.Generic;
using System.CommandLine;


namespace Torque;




public class LinkCommand : Command
{
    public Argument<IEnumerable<FileInfo>> Files { get; }

    public Option<string> Output { get; }




    public LinkCommand() : base("link", "Links object files into a binary executable/library.")
    {
        Add(Files = new Argument<IEnumerable<FileInfo>>("files")
        {
            Arity = ArgumentArity.OneOrMore
        });

        Add(Output = new Option<string>("--output", "-o"));

        SetAction(Callback);
    }


    private void Callback(ParseResult result)
        => Torque.Link(GetOptions(result));




    public TorqueLinkOptions GetOptions(ParseResult result) => new TorqueLinkOptions
    {
        Files = result.GetRequiredValue(Files),

        Output = result.GetValue(Output)
    };
}
