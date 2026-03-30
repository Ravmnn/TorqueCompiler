using System.IO;
using System.Collections.Generic;

using Torque.Compiler.AST.Statements;
using Torque.Compiler.Types;
using Torque.Compiler.BoundAST.Statements;


namespace Torque.Compiler;




public readonly struct Module(
    string path,
    IReadOnlyList<BoundStatement> statements,
    IReadOnlyList<Statement> syntaxStatements,
    Scope scope,
    DeclaredTypeManager declaredTypes,
    List<Module>? importedModules = null)
{
    public string Path { get; } = path;
    public FileInfo FileInfo { get; } = new FileInfo(path);
    public IReadOnlyList<BoundStatement> Statements { get; init; } = statements;
    public IReadOnlyList<Statement> SyntaxStatements { get; init; } = syntaxStatements;
    public Scope Scope { get; init; } = scope;
    public DeclaredTypeManager DeclaredTypes { get; init; } = declaredTypes;
    public List<Module> ImportedModules { get; init; } = importedModules ?? [];




    public void Deconstruct(
        out string path,
        out IReadOnlyList<BoundStatement> statements,
        out IReadOnlyList<Statement> syntaxStatements,
        out Scope scope,
        out DeclaredTypeManager declaredTypes,
        out List<Module> importedModules
    )
    {
        path = Path;
        statements = Statements;
        syntaxStatements = SyntaxStatements;
        scope = Scope;
        declaredTypes = DeclaredTypes;
        importedModules = ImportedModules;
    }
}
