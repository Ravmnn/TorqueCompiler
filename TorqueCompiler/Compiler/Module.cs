using System.Collections.Generic;

using Torque.Compiler.AST.Statements;
using Torque.Compiler.Types;
using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler;




public readonly struct Module(
    IReadOnlyList<BoundStatement> statements,
    IReadOnlyList<Statement> syntaxStatements,
    Scope scope,
    DeclaredTypeManager declaredTypes,
    IList<Module>? importedModules = null)
{
    public IReadOnlyList<BoundStatement> Statements { get; init; } = statements;
    public IReadOnlyList<Statement> SyntaxStatements { get; init; } = syntaxStatements;
    public Scope Scope { get; init; } = scope;
    public DeclaredTypeManager DeclaredTypes { get; init; } = declaredTypes;
    public IList<Module> ImportedModules { get; init; } = importedModules ?? [];




    public void Deconstruct(out IReadOnlyList<BoundStatement> statements,
        out IReadOnlyList<Statement> syntaxStatements,
        out Scope scope,
        out DeclaredTypeManager declaredTypes,
        out IList<Module> importedModules
    )
    {
        statements = Statements;
        syntaxStatements = SyntaxStatements;
        scope = Scope;
        declaredTypes = DeclaredTypes;
        importedModules = ImportedModules;
    }
}
