using System.Collections.Generic;


namespace Torque.Compiler.Tokens;




public interface IModificable
{
    IReadOnlyList<Modifier> Modifiers { get; set; }
    ModifierTarget ThisTargetIdentity { get; }
}
