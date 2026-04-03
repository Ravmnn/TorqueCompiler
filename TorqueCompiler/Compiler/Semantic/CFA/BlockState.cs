namespace Torque.Compiler.Semantic.CFA;


public struct BlockState
{
    public bool IsReachable { get; set; }
    public bool Returns { get; set; }
}
