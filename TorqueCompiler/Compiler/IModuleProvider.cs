namespace Torque.Compiler;




public interface IModuleProvider
{
    ModuleInfo LoadModuleById(string id);
    ModuleInfo LoadModuleByPath(string path);
}
