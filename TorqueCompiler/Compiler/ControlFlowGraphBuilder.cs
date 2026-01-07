using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




// TODO: when add control flow statements like if and while, change this to support them
public class ControlFlowGraphBuilder(IReadOnlyList<BoundFunctionDeclarationStatement> functionDeclarations) : IBoundStatementProcessor
{
    private readonly List<ControlFlowGraph> _graphs = [];
    private ControlFlowGraph? _currentGraph;

    private BasicBlock _currentBlock = null!;
    private int _blockCounter;

    private bool _reachable = true;


    public IReadOnlyList<BoundFunctionDeclarationStatement> FunctionDeclarations { get; } = functionDeclarations;




    public IReadOnlyList<ControlFlowGraph> Build()
    {
        Reset();

        foreach (var function in FunctionDeclarations)
        {
            ResetBlocks();

            Process(function);
            _graphs.Add(_currentGraph!);
        }

        RemoveEmptyBlocks();

        return _graphs;
    }


    private void Reset()
    {

        _currentGraph = null;
        _graphs.Clear();

        ResetBlocks();
    }


    private void ResetBlocks()
    {
        _currentBlock = null!;
        _blockCounter = 0;

        _reachable = true;
    }


    private void RemoveEmptyBlocks()
    {
        foreach (var graph in _graphs)
            foreach (var block in graph.Blocks.ToArray())
                if (block.Statements.Count == 0)
                    graph.Blocks.Remove(block);
    }




    public void Process(BoundStatement statement)
        => statement.Process(this);




    public void ProcessExpression(BoundExpressionStatement statement)
        => AddStatementToCurrentBlock(statement);




    public void ProcessDeclaration(BoundDeclarationStatement statement)
        => AddStatementToCurrentBlock(statement);




    public void ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
    {
        _currentGraph = new ControlFlowGraph(statement);
        _currentGraph.Entry = AttachNewBlock();

        Process(statement.Body);
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        AddStatementToCurrentBlock(statement);

        _currentBlock.State.HasReturn = true;
        _reachable = false;

        _currentBlock = NewBlockFromLast();
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }




    public void ProcessIf(BoundIfStatement statement)
    {
        AddStatementToCurrentBlock(statement);

        var origin = _currentBlock;
        var joinPredecessors = new List<BasicBlock>();

        AttachNewBlockFromAndProcess(statement.ThenStatement, origin);
        joinPredecessors.Add(_currentBlock);

        if (statement.ElseStatement is not null)
        {
            AttachNewBlockFromAndProcess(statement.ElseStatement, origin);
            joinPredecessors.Add(_currentBlock);
        }

        _currentBlock = NewBlockFrom(joinPredecessors);
    }








    private void AddStatementToCurrentBlock(BoundStatement statement)
        => _currentBlock.Statements.Add(statement);




    private BasicBlock AttachNewBlockFromAndProcess(BoundStatement statement, params IReadOnlyList<BasicBlock> predecessors)
    {
        AttachNewBlockFrom(predecessors);
        Process(statement);

        return _currentBlock;
    }




    private BasicBlock AttachNewBlockFromLast()
        => _currentBlock = NewBlockFromLast();


    private BasicBlock AttachNewBlockFrom(params IReadOnlyList<BasicBlock> predecessors)
        => _currentBlock = NewBlockFrom(predecessors);


    private BasicBlock AttachNewBlock()
        => _currentBlock = NewBlock();




    private BasicBlock NewBlockFromLast()
        => NewBlockFrom(_currentBlock);


    private BasicBlock NewBlockFrom(params IReadOnlyList<BasicBlock> predecessors)
    {
        var block = NewBlock();

        foreach (var predecessor in predecessors)
        {
            predecessor.Successors.Add(block);
            block.Predecessor.Add(predecessor);
        }

        return block;
    }


    private BasicBlock NewBlock()
    {
        var block = new BasicBlock($"B{_blockCounter++}");
        _currentGraph!.Blocks.Add(block);

        block.State.Reachable = _reachable;

        return block;
    }
}
