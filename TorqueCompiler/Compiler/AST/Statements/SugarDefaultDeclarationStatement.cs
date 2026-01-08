using Torque.Compiler.Types;
using Torque.Compiler.Symbols;


namespace Torque.Compiler.AST.Statements;




public class SugarDefaultDeclarationStatement(TypeSyntax type, SymbolSyntax name) : SugarStatement(name.Location)
{
    public TypeSyntax Type { get; } = type;
    public SymbolSyntax Name { get; } = name;




    public override Statement Process(ISugarStatementProcessor processor)
        => processor.ProcessDefaultDeclaration(this);
}
