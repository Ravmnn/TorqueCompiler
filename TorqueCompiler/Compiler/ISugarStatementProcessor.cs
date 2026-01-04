namespace Torque.Compiler;




public interface ISugarStatementProcessor
{
    Statement Process(Statement statement);

    Statement ProcessDefaultDeclaration(SugarDefaultDeclarationStatement statement);
}
