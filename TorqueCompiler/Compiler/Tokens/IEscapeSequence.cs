namespace Torque.Compiler.Tokens;




public interface IEscapeSequence
{
    char Name { get; }
    int Arity { get; }


    byte GetByte(string argument);
}
