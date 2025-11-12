using System.Collections.Generic;
using System.Linq;


namespace Torque.Compiler;




public enum BitMode
{
    Bits16 = 16,
    Bits32 = 32,
    Bits64 = 64
}




public class TorqueCompiler : IStatementProcessor, IExpressionProcessor
{
    private const string DefaultEntryPoint = "main";

    private const string StackPointerRegister = "rsp";
    private const string StackFrameRegister = "rbp";




    private readonly AssemblyGenerator _assembly = new AssemblyGenerator();
    private readonly AssemblyGenerator _data = new AssemblyGenerator();
    private readonly AssemblyGenerator _start = new AssemblyGenerator();
    private readonly AssemblyGenerator _code = new AssemblyGenerator();

    private AssemblyGenerator _generator;


    private ASTPrinter _printer = new ASTPrinter
    {
        IgnoreBlocks = true,
        NoNewlines = true
    };




    public Statement[] Statements { get; }

    public BitMode BitMode { get; init; } = BitMode.Bits32;
    public string EntryPoint { get; init; } = DefaultEntryPoint;




    public TorqueCompiler(IEnumerable<Statement> statements)
    {
        _generator = _assembly;


        Statements = statements.ToArray();
    }




    public string Compile()
    {
        return string.Empty;
    }




    public void Process(Expression expression)
        => expression.Process(this);


    public void Process(Statement statement)
        => statement.Process(this);




    public void ProcessExpression(ExpressionStatement statement)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessDeclaration(DeclarationStatement statement)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessFunctionDeclaration(FunctionDeclarationStatement statement)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessReturn(ReturnStatement statement)
    {
        throw new System.NotImplementedException();
    }


    public void ProcessBlock(BlockStatement statement)
    {
        throw new System.NotImplementedException();
    }




    public void ProcessLiteral(LiteralExpression expression)
    {
        throw new System.NotImplementedException();
    }

    public void ProcessBinary(BinaryExpression expression)
    {
        throw new System.NotImplementedException();
    }

    public void ProcessGrouping(GroupingExpression expression)
    {
        throw new System.NotImplementedException();
    }

    public void ProcessIdentifier(IdentifierExpression expression)
    {
        throw new System.NotImplementedException();
    }

    public void ProcessCall(CallExpression expression)
    {
        throw new System.NotImplementedException();
    }

    public void ProcessCast(CastExpression expression)
    {
        throw new System.NotImplementedException();
    }
}
