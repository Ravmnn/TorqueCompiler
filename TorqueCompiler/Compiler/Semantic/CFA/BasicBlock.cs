using System.Collections.Generic;

using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler.Semantic.CFA;




public sealed class BasicBlock(string name)
{
    public string Name { get; } = name;

    public List<BoundStatement> Statements { get; } = [];

    public List<BasicBlock> Successors { get; } = [];
    public List<BasicBlock> Predecessors { get; } = [];

    private BlockState _state;
    public ref BlockState State { get => ref _state; }




    public override string ToString() => Name;
}