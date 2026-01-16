using Torque.Compiler.Target;


namespace Torque.CommandLine;




public static class TargetTripleExtensions
{
    extension(TargetTriple)
    {
        public static TargetTriple FromCompileSettings(CompileCommandSettings settings)
            => new TargetTriple
            {
                Architecture = settings.Architecture,
                OperationalSystem = settings.OperationalSystem,
                Environment = settings.Environment,
                Vendor = settings.Vendor
            };
    }
}
