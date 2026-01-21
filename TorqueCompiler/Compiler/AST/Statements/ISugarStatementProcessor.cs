namespace Torque.Compiler.AST.Statements;




public interface ISugarStatementProcessor
{
    Statement Process(Statement statement);

    Statement ProcessDefaultDeclaration(SugarDefaultDeclarationStatement statement);
    Statement ProcessLoop(SugarLoopStatement statement);
    Statement ProcessFor(SugarForStatement statement);
}
