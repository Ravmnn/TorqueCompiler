using System;


namespace Torque.Compiler;




public abstract class CompilerControlException : Exception;


public class UnreachableCodeControl : CompilerControlException;
