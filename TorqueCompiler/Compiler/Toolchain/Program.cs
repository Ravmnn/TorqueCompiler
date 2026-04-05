namespace Torque.Compiler.Toolchain;




public abstract class Program
{
    public abstract string ProgramPath { get; }




    public virtual void Run()
        => ProcessInvoke.ExecuteAndWait(ProgramPath, GetCommandLineArguments());


    public abstract string GetCommandLineArguments();
}
