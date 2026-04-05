namespace Torque.Compiler.CodeGen;




public interface IIREmitter
{
    void EmitModule(Module module, IRGenerationOptions options);
}
