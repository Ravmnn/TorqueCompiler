using Torque.Compiler.Types;
using Torque.Compiler.Symbols;
using Torque.Compiler.AST.Expressions;


namespace Torque.Compiler.AST.Statements;




public class DeclarationStatement(TypeSyntax type, SymbolSyntax name, Expression value) : Statement(name.Location)
{
    public TypeSyntax Type { get; } = type;
    public SymbolSyntax Name { get; } = name;
    public Expression Value { get; } = value;




    public override void Process(IStatementProcessor processor)
        => processor.ProcessDeclaration(this);


    public override T Process<T>(IStatementProcessor<T> processor)
        => processor.ProcessDeclaration(this);
}
