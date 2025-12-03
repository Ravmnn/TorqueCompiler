using System.CommandLine;


namespace Torque;

// TODO: use Spectre.Console.Cli


public class TorqueRootCommand : RootCommand
{
    public ParseResult Result { get; }


    public CompileCommand Compile { get; }
    public LinkCommand Link { get; }




    public TorqueRootCommand(string[] args) : base("The official AOT compiler for the Torque programming language.")
    {
        Add(Compile = new CompileCommand());
        Add(Link = new LinkCommand());


        Result = Parse(args);
    }
}
