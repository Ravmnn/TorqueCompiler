using System.Collections.Generic;


namespace Torque.Compiler.Tokens;




public interface IModificable
{
    IList<Modifier> Modifiers { get; set; }
    ModifierTarget ThisTargetIdentity { get; }
}
