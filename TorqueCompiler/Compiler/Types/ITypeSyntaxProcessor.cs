namespace Torque.Compiler.Types;




public interface ITypeSyntaxProcessor<out T>
{
    T Process(TypeSyntax type);

    T ProcessBase(BaseTypeSyntax type);
    T ProcessPointer(PointerTypeSyntax type);
    T ProcessFunction(FunctionTypeSyntax type);
    T ProcessStruct(StructTypeSyntax type);
}
