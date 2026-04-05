using System;
using System.Collections.Generic;

using Torque.Compiler.BoundAST.Statements;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.Semantic.CFA;




public sealed class ControlFlowGraphBuilder : IBoundStatementProcessor<BasicBlock>
{
    private List<BasicBlock> _blocks = [];
    private BasicBlock _current = null!;




    public static IReadOnlyCollection<ControlFlowGraph> BuildFromFunctionDeclarations(IReadOnlyCollection<BoundFunctionDeclarationStatement> functions)
    {
        var builder = new ControlFlowGraphBuilder();
        var graphs = new List<ControlFlowGraph>();

        foreach (var function in functions)
            if (function.Body is not null)
                graphs.Add(BuildFromFunctionDeclaration(builder, function));

        return graphs;
    }


    private static ControlFlowGraph BuildFromFunctionDeclaration(ControlFlowGraphBuilder builder, BoundFunctionDeclarationStatement function)
    {
        var graph = builder.Build(function.Body!, function.Location);
        graph.Id = function.Symbol.Name;

        if (function.FunctionSymbol.Type.ReturnType.IsVoid)
            graph.IgnoreAllPathReturnsAnalysis = true;

        return graph;
    }




    public ControlFlowGraph Build(BoundStatement root, Span? location = null)
    {
        _blocks = [];

        var entry = _current = NewBlock("entry");
        Process(root);

        return new ControlFlowGraph(entry, _blocks, location ?? root.Location);
    }




    public BasicBlock Process(BoundStatement statement)
        => statement.Process(this);


    public BasicBlock ProcessExpression(BoundExpressionStatement statement) => AddToCurrent(statement);
    public BasicBlock ProcessDeclaration(BoundVariableDeclarationStatement statement) => AddToCurrent(statement);
    public BasicBlock ProcessFunctionDeclaration(BoundFunctionDeclarationStatement statement)
        => throw new InvalidOperationException("Cannot process a function declaration in the CFG builder");


    public BasicBlock ProcessReturn(BoundReturnStatement statement)
    {
        AddToCurrent(statement);
        return _current = NewBlock("return_unreachable");
    }


    public BasicBlock ProcessBlock(BoundBlockStatement statement)
    {
        foreach (var subStatement in statement.Statements)
            Process(subStatement);

        return _current;
    }


    public BasicBlock ProcessIf(BoundIfStatement statement)
    {
        var thenBlock = NewBlock("then");
        var mergeBlock = NewBlock("merge");
        BasicBlock? elseBlock = null;

        Connect(_current, thenBlock);

        if (statement.ElseStatement is not null)
        {
            elseBlock = NewBlock("else");
            Connect(_current, elseBlock);
        }
        else
            Connect(_current, mergeBlock);

        ProcessBranch(statement.ThenStatement, thenBlock, mergeBlock);

        if (statement.ElseStatement is not null)
            ProcessBranch(statement.ElseStatement, elseBlock!, mergeBlock);

        return mergeBlock;
    }


    public BasicBlock ProcessWhile(BoundWhileStatement statement)
    {
        var bodyBlock = NewBlock("loop");
        var exitBlock = NewBlock("exit");

        Connect(_current, bodyBlock);
        Connect(_current, exitBlock);

        ProcessBranch(statement.Loop, bodyBlock, exitBlock);

        _current = exitBlock;
        return exitBlock;
    }


    public BasicBlock ProcessContinue(BoundContinueStatement statement) => AddToCurrent(statement);

    public BasicBlock ProcessBreak(BoundBreakStatement statement) => AddToCurrent(statement);




    private void ProcessBranch(BoundStatement statement, BasicBlock branch, BasicBlock exit)
    {
        _current = branch;

        var thenEnd = Process(statement);
        Connect(thenEnd, exit);

        _current = exit;
    }




    private BasicBlock NewBlock(string name)
    {
        var block = new BasicBlock(name);
        _blocks.Add(block);
        return block;
    }


    private BasicBlock AddToCurrent(BoundStatement statement)
    {
        _current.Statements.Add(statement);
        return _current;
    }


    private static void Connect(BasicBlock from, BasicBlock to)
    {
        from.Successors.Add(to);
        to.Predecessors.Add(from);
    }
}