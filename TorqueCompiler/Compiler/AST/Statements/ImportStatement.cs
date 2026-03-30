using System.Collections.Generic;

using Torque.Compiler.Symbols;
using Torque.Compiler.Tokens;


namespace Torque.Compiler.AST.Statements;




public class ImportStatement(IReadOnlyList<SymbolSyntax> path, Span location) : Statement(location)
{
    public IReadOnlyList<SymbolSyntax> Path { get; } = path;

    public override bool CanBeInFileScope => true;
    public override bool CanBeInFunctionScope => false;


    public override void Process(IStatementProcessor processor)
        => processor.ProcessImport(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessImport(this);




    public string GetModuleRelativePath()
    {
        var stringPath = string.Join('/', Path);
        var fullPath = stringPath + CommandLine.Torque.FileExtension;

        return fullPath;
    }
}
