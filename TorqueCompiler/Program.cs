namespace Torque;




class Program
{
    private static void Main(string[] args)
    {
        var root = new TorqueRootCommand(args);
        root.Result.Invoke();
    }
}
