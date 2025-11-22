namespace Torque;




class Program
{
    private static void Main(string[] args)
    {
        //args = "compile /home/marvin/Documentos/program/csharp/TorqueCompiler/examples/test.tor".Split(' ');

        var root = new TorqueRootCommand(args);
        root.Result.Invoke();
    }
}
