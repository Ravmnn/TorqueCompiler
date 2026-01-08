namespace Torque.Compiler.AST.Statements;




public interface ISugarStatementProcessor
{
    Statement Process(Statement statement);

    Statement ProcessDefaultDeclaration(SugarDefaultDeclarationStatement statement);
}
