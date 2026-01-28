using System;


namespace Torque.CommandLine.Exceptions;




public class InterruptCompileException : Exception
{
    public InterruptCompileException()
    {
    }

    public InterruptCompileException(Exception inner) : base(null, inner)
    {
    }
}
