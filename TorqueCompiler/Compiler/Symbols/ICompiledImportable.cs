namespace Torque.Compiler.Symbols;




public interface ICompiledImportable
{
    void Process(IImportableProcessor processor);
}