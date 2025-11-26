namespace Torque;




class Program
{
    private static void Main(string[] args)
    {
        //args = "compile /home/marvin/Documentos/program/csharp/TorqueCompiler/examples/test.tor --debug --print-llvm".Split(' ');

        var root = new TorqueRootCommand(args);
        root.Result.Invoke();
    }
}
