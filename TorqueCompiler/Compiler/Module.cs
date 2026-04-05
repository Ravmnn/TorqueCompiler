using System.IO;
using System.Collections.Generic;

using Torque.Compiler.AST.Statements;
using Torque.Compiler.Types;
using Torque.Compiler.BoundAST.Statements;
using LLVMSharp.Interop;


namespace Torque.Compiler;




public class Module(
    string path,
    IReadOnlyList<BoundStatement> statements,
    IReadOnlyList<Statement> syntaxStatements,
    Scope scope,
    DeclaredTypeManager declaredTypes,
    List<Module>? importedModules = null)
{
    public string Path { get; } = path;
    public SourceCode SourceCode { get; } = new SourceCode(new FileInfo(path));
    public IReadOnlyList<BoundStatement> Statements { get; init; } = statements;
    public IReadOnlyList<Statement> SyntaxStatements { get; init; } = syntaxStatements;
    public Scope Scope { get; init; } = scope;
    public DeclaredTypeManager DeclaredTypes { get; init; } = declaredTypes;
    public List<Module> ImportedModules { get; init; } = importedModules ?? [];

    public LLVMModuleRef? LLVMModule { get; set; }
}
