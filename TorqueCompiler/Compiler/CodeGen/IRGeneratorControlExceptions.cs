using System;


namespace Torque.Compiler.CodeGen;




internal abstract class IRGeneratorControlException : Exception;


internal class UnreachableCodeControl : IRGeneratorControlException;
