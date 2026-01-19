namespace Torque.CommandLine.Toolchain;




public abstract class Program
{
    public abstract string ProgramPath { get; }




    public virtual void Run()
        => ProcessInvoke.ExecuteAndWait(ProgramPath, GetCommandLineArguments());


    public abstract string GetCommandLineArguments();
}
