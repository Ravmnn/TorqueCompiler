using System.Collections.Generic;


namespace Torque.Compiler.Tokens;




// TODO: Maybe rename to IDeclaration and move to AST.Statements
public interface IModificable
{
    IReadOnlyList<Modifier> Modifiers { get; set; }
    ModifierTarget ThisTargetIdentity { get; }
}
