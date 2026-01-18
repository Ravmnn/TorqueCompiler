using System;
using System.Collections.Generic;


namespace Torque.Compiler.Tokens;




[Flags]
public enum ModifierTarget
{
    Field = 1 << 0,
    LocalVariable = 1 << 1,
    Parameter = 1 << 2,

    Function = 1 << 3,
    Method = 1 << 4,

    Variables = Field | LocalVariable | Parameter,
    Functions = Function | Method
}




public static class ModifiersTarget
{
    private static readonly IReadOnlyDictionary<TokenType, IReadOnlyList<ModifierTarget>> _targets = new Dictionary<TokenType, IReadOnlyList<ModifierTarget>>
    {
        { TokenType.KwExternal, [ModifierTarget.Function] }
    };




    public static IReadOnlyList<ModifierTarget> GetFor(TokenType modifier)
        => _targets[modifier];
}
