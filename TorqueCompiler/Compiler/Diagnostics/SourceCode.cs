namespace Torque.Compiler.Diagnostics;




public static class SourceCode
{
    public static string? Source { get; set; }
    public static string[]? SourceLines => Source?.Split('\n');

    public static string? FileName { get; set; }




    public static string GetLine(int line)
        => SourceLines![line - 1];
}
