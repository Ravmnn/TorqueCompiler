using System.Collections.Generic;

using Torque.Compiler.Types;
using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler;




public readonly record struct ModuleContext(IReadOnlyList<BoundStatement> Statements, Scope Scope,
    DeclaredTypeManager DeclaredTypes);
