using System.Collections.Generic;


namespace Torque.Compiler;




public class ControlFlowGraph(BoundFunctionDeclarationStatement functionDeclaration)
{
    public BoundFunctionDeclarationStatement FunctionDeclaration { get; } = functionDeclaration;

    public BasicBlock Entry { get; set; } = null!;
    public List<BasicBlock> Blocks { get; } = [];
}
