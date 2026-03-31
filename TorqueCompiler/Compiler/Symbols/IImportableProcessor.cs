using Torque.Compiler.Types;


namespace Torque.Compiler.Symbols;




public interface IImportableProcessor
{
    void ProcessImportable(ICompiledImportable importable);

    void ProcessFunctionImport(FunctionSymbol symbol);
    void ProcessStructImport(StructTypeDeclaration declaration);
}
