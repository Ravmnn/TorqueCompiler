namespace Torque.Compiler.Tokens;




public struct SingleEscapeSequence(char name, byte value) : IEscapeSequence
{
    public char Name { get; } = name;
    public int Arity => 0;

    public byte Value { get; } = value;


    public byte GetByte(string argument) => Value;
}
