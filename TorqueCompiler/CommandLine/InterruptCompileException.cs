using System;


namespace Torque.CommandLine;




public class InterruptCompileException : Exception
{
    public InterruptCompileException()
    {
    }

    public InterruptCompileException(Exception inner) : base(null, inner)
    {
    }
}
