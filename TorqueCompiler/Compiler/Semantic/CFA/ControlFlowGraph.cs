using System.Collections.Generic;

using Torque.Compiler.Tokens;


namespace Torque.Compiler.Semantic.CFA;




public sealed class ControlFlowGraph(BasicBlock entry, List<BasicBlock> blocks, Span location)
{
    public BasicBlock Entry { get; } = entry;
    public List<BasicBlock> Blocks { get; } = blocks;

    public Span Location { get; set; } = location;

    public string? Id { get; set; }
    public bool IgnoreAllPathReturnsAnalysis { get; set; }




    public override string ToString()
        => (Id ?? base.ToString())!;
}