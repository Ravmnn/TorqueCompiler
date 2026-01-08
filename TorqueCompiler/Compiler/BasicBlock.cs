using System.Collections.Generic;

using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler;




public record struct BlockState
{
    public bool HasReturn { get; set; }
}


public class BasicBlock(string name)
{
    public string Name { get; init; } = name;

    private BlockState _state;
    public ref BlockState State => ref _state;

    public List<BoundStatement> Statements { get; } = [];

    public List<BasicBlock> Predecessor { get; } = [];
    public List<BasicBlock> Successors { get; } = [];
}
