using System;
using System.Collections.Generic;


namespace Torque.Compiler;




// TODO: when add control flow statements like if and while, change this to support them
public class ControlFlowGraphBuilder(IReadOnlyList<BoundFunctionDeclarationStatement> functionDeclarations) : IBoundStatementProcessor
{
    private readonly List<ControlFlowGraph> _graphs = [];
    private ControlFlowGraph? _currentGraph;

    private readonly Stack<BasicBlock> _blocks = [];
    private int _blockCounter;

    private bool _reachable = true;

    private BasicBlock CurrentBlock => _blocks.Peek();


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
        _blocks.Clear();
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
        _currentGraph.Entry = NewBlock();
        _blocks.Push(_currentGraph.Entry);

        Process(statement.Body);
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        AddStatementToCurrentBlock(statement);
        CurrentBlock.State.HasReturn = true;
        _reachable = false;

        _blocks.Push(NewBlock());
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }




    private void ProcessNewBlock(Action action)
    {
        _blocks.Push(NewBlock());
        action();
        _blocks.Pop();
    }


    private void AddStatementToCurrentBlock(BoundStatement statement)
    {
        CurrentBlock.Statements.Add(statement);
    }


    private BasicBlock NewBlock()
    {
        var block = new BasicBlock($"B{_blockCounter++}");
        _currentGraph!.Blocks.Add(block);

        block.State.Reachable = _reachable;

        return block;
    }
}
