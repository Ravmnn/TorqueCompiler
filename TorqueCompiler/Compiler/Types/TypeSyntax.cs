using Torque.Compiler.Symbols;


namespace Torque.Compiler.Types;




public abstract class TypeSyntax
{
    public abstract BaseTypeSyntax BaseType { get; }
    public SymbolSyntax SymbolSyntax => BaseType.TypeSymbol;




    public abstract T Process<T>(ITypeSyntaxProcessor<T> processor);




    public override string ToString()
        => BaseType.TypeSymbol.Name;
}
