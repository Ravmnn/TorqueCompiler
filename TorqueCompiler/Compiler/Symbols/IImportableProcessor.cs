namespace Torque.Compiler.Symbols;




public interface IImportableProcessor
{
    void ProcessImportable(IImportable importable);

    void ProcessFunctionImport(FunctionSymbol symbol);
}
