namespace Torque.Compiler;




public static class BooleanExtensions
{
    extension(bool value)
    {
        public int BoolToInt()
            => value ? 1 : 0;
    }
}
