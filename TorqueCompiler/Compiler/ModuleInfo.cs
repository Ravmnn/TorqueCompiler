using System;


namespace Torque.Compiler;




public enum ModuleLoadState
{
    NonExistent,
    Loading,
    Loaded
}


public readonly record struct ModuleInfo(Module? Module, ModuleLoadState State)
{
    public static ModuleInfo NonExistent => new ModuleInfo(null, ModuleLoadState.NonExistent);
}
