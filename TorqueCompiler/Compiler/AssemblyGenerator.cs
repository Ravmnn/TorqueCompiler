using System.Text;


namespace Torque.Compiler;




public class AssemblyGenerator
{
    private readonly StringBuilder _builder = new StringBuilder();
    private bool _indent;

    private string _comment = string.Empty;




    public void EnableIndent() => _indent = true;
    public void DisableIndent() => _indent = false;


    public string Newline(uint amount = 1)
    {
        var newline = string.Empty;

        if (_comment.Length > 0)
        {
            newline = $" ; {_comment}";
            _comment = string.Empty;
        }

        return newline + new string('\n', (int)amount);
    }


    public string Tab()
        => _indent ? "    " : string.Empty;




    public void NasmDirective(string directive, object value)
        => _builder.Append($"{directive} {value}");


    public void NasmBits(BitMode bitMode) => NasmDirective("bits", (int)bitMode);
    public void NasmSection(string name) => NasmDirective("section", name);
    public void NasmGlobal(string identifier) => NasmDirective("global", identifier);




    public void Comment(string content) => _comment = content;
    public void InstantComment(string content) => _builder.Append($"{Tab()}; {content}{Newline()}");


    public void Label(string name) => _builder.Append($"{name}:");
    public void LocalLabel(string name) => Label('.' + name);


    public void Instruction(string instruction, object? op1 = null, object? op2 = null)
    {
        _builder.Append(instruction);

        if (op1 is not null)
            _builder.Append($" {op1}");

        if (op2 is not null)
            _builder.Append($", {op2}");

        _builder.Append(Newline());
    }




    public void ClearRegister(string register)
        => Instruction("xor", register, register);


    public void Syscall(object code, object? arg1 = null, object? arg2 = null, object? arg3 = null)
    {
        Instruction("mov", "rax", code);

        if (arg1 is not null)
            Instruction("mov", "rdi", arg1);

        if (arg2 is not null)
            Instruction("mov", "rsi", arg2);

        if (arg3 is not null)
            Instruction("mov", "rdx", arg3);

        Instruction("syscall");
    }


    public void SyscallExit(object exitCode)
        => Syscall(60, exitCode);




    public static string Adressing(string register, int? displacement = null)
    {
        var builder = new StringBuilder();
        var displacementString = string.Empty;

        if (displacement is not null)
            displacementString = displacement > 0 ? $" + {displacement}" : $" - {displacement}";

        builder.Append($"[{register}{displacementString}]");

        return builder.ToString();
    }
}
