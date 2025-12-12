using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public class ControlFlowGraph
{
    public BasicBlock Entry { get; set; } = null!;
    public List<BasicBlock> Blocks { get; } = [];
}




public class ControlFlowGraphBuilder(IReadOnlyList<BoundFunctionDeclarationStatement> functionDeclarations)
{
    private int _blockCounter;


    public IReadOnlyList<BoundFunctionDeclarationStatement> FunctionDeclarations { get; } = functionDeclarations;

    public ControlFlowGraph? ControlFlowGraph { get; private set; }




    public IReadOnlyList<ControlFlowGraph> BuildAll()
        => (from functionDeclaration in FunctionDeclarations select Build(functionDeclaration)).ToArray();


    private ControlFlowGraph Build(BoundFunctionDeclarationStatement functionDeclaration)
    {
        ControlFlowGraph = new ControlFlowGraph();
        ControlFlowGraph.Entry = NewBlock();

        BuildBlock(functionDeclaration.Body.Statements, ControlFlowGraph.Entry);

        return ControlFlowGraph;
    }


    private BasicBlock? BuildBlock(IReadOnlyList<BoundStatement> statements, BasicBlock? current)
    {
        foreach (var statement in statements)
        {
            switch (statement)
            {
                case BoundReturnStatement @return:
                    current!.Statements.Add(@return);
                    current = null;
                    break;

                case BoundBlockStatement block:
                    current = BuildBlock(block.Statements, current);
                    break;

                default:
                    current!.Statements.Add(statement);
                    break;
            }

            if (current is null)
                return null;
        }

        return current;
    }


    private BasicBlock? HandleBlock(BoundBlockStatement statement, BasicBlock current)
    {
        var block = NewBlock();
        var join = NewBlock();

        current.Successors.Add(block);

        if (BuildBlock(statement.Statements, block) is null)
            return null;

        block.Successors.Add(join);
        return join;
    }




    private BasicBlock NewBlock()
    {
        var block = new BasicBlock($"B{_blockCounter++}");
        ControlFlowGraph!.Blocks.Add(block);

        return block;
    }
}
