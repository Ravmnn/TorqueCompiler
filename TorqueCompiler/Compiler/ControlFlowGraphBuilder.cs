using System.Collections.Generic;

using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler;




public class ControlFlowGraphBuilder(IReadOnlyList<BoundFunctionDeclarationStatement> functionDeclarations) : IBoundStatementProcessor
{
    private readonly List<ControlFlowGraph> _graphs = [];
    private ControlFlowGraph? _currentGraph;

    private BasicBlock _currentBlock = null!;
    private int _blockCounter;


    public IReadOnlyList<BoundFunctionDeclarationStatement> FunctionDeclarations { get; } = functionDeclarations;




    public IReadOnlyList<ControlFlowGraph> Build()
    {
        Reset();
        BuildAllFunctions();
        RemoveEmptyBlocks();

        return _graphs;
    }


    private void BuildAllFunctions()
    {
        foreach (var function in FunctionDeclarations)
        {
            if (function.IsExternal)
                continue;

            ResetBlocks();
            Process(function);
            _graphs.Add(_currentGraph!);
        }
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

        Process(statement.Body!);
    }




    public void ProcessReturn(BoundReturnStatement statement)
    {
        AddStatementToCurrentBlock(statement);

        _currentBlock.State.HasReturn = true;
        _currentBlock = NewBlockWithPredecessorAsLastBlock();
    }




    public void ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var blockStatement in statement.Statements)
            Process(blockStatement);
    }




    public void ProcessIf(BoundIfStatement statement)
    {
        AddStatementToCurrentBlock(statement);
        var joinPredecessors = new List<BasicBlock>();

        ProcessThenAndElseStatements(statement, _currentBlock, joinPredecessors);

        _currentBlock = NewBlockWithPredecessors(joinPredecessors);
    }


    private void ProcessThenAndElseStatements(BoundIfStatement statement, BasicBlock origin, List<BasicBlock> joinPredecessors)
    {
        joinPredecessors.Add(AttachNewBlockWithPredecessorsAndProcess(statement.ThenStatement, origin));

        if (statement.ElseStatement is not null)
            joinPredecessors.Add(AttachNewBlockWithPredecessorsAndProcess(statement.ElseStatement, origin));
        else
            joinPredecessors.Add(origin);
    }




    private void AddStatementToCurrentBlock(BoundStatement statement)
        => _currentBlock.Statements.Add(statement);




    private BasicBlock AttachNewBlockWithPredecessorsAndProcess(BoundStatement statement, params IReadOnlyList<BasicBlock> predecessors)
    {
        AttachNewBlockWithPredecessors(predecessors);
        Process(statement);

        return _currentBlock;
    }




    private BasicBlock AttachNewBlockWithPredecessorsAsLastBlock()
        => _currentBlock = NewBlockWithPredecessorAsLastBlock();


    private BasicBlock AttachNewBlockWithPredecessors(params IReadOnlyList<BasicBlock> predecessors)
        => _currentBlock = NewBlockWithPredecessors(predecessors);


    private BasicBlock AttachNewBlock()
        => _currentBlock = NewBlock();




    private BasicBlock NewBlockWithPredecessorAsLastBlock()
        => NewBlockWithPredecessors(_currentBlock);


    private BasicBlock NewBlockWithPredecessors(params IReadOnlyList<BasicBlock> predecessors)
    {
        var block = NewBlock();

        foreach (var predecessor in predecessors)
            AddPredecessorToBlock(predecessor, block);

        return block;
    }


    private static void AddPredecessorToBlock(BasicBlock predecessor, BasicBlock block)
    {
        predecessor.Successors.Add(block);
        block.Predecessor.Add(predecessor);
    }


    private BasicBlock NewBlock()
    {
        var block = new BasicBlock($"B{_blockCounter++}");
        _currentGraph!.Blocks.Add(block);

        return block;
    }
}
