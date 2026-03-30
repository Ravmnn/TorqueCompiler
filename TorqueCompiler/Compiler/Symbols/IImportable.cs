namespace Torque.Compiler.Symbols;




public interface IImportable
{
    bool CanBeCompiled { get; }


    void Process(IImportableProcessor processor);
}