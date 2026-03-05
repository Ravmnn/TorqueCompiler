using Torque.Compiler.Tokens;
using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public class BaseTypeSyntax(SymbolSyntax typeSymbol) : TypeSyntax
{
    public override BaseTypeSyntax BaseType => this;


    public SymbolSyntax TypeSymbol { get; } = typeSymbol;
    public bool IsPrimitiveType => Keywords.PrimitiveTypes.ContainsKey(TypeSymbol.Name);




    public override T Process<T>(ITypeSyntaxProcessor<T> processor)
        => processor.ProcessBase(this);
}
