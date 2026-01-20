using System;


namespace Torque.Compiler;




public abstract class CompilerControlException : Exception;


public class UnreachableCodeControl : CompilerControlException;
public class BreakLoopControl : CompilerControlException;
public class ContinueLoopControl : CompilerControlException;
