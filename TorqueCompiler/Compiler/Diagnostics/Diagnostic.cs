using System;


namespace Torque.Compiler.Diagnostics;




public enum DiagnosticSeverity
{
    Info,
    Warning,
    Error
}


public enum DiagnosticScope
{
    Lexer,
    Parser,
    SymbolResolver,
    Compiler
}




public readonly partial struct Diagnostic()
{
    public required int Code { get; init; }
    public required DiagnosticScope Scope { get; init; }
    public required DiagnosticSeverity Severity { get; init; }

    public required string MessageId { get; init; }



    public string[] Arguments { get; init; } = [];
    public TokenLocation? Location { get; init; }




    public static Diagnostic FromCatalog<T>(int code, string[]? arguments = null, TokenLocation? location = null)
        where T : Enum
    {
        var (item, scope, severity) = GetFromCatalog<T>(code);

        return new Diagnostic
        {
            Code = code,
            Scope = scope,
            Severity = severity,
            MessageId = item.ToString(),
            Arguments = arguments ?? [],
            Location = location
        };
    }




    public string GetMessage() => Scope switch
    {
        DiagnosticScope.Lexer => LexerDiagnostics.ResourceManager.GetString(MessageId)!,
        DiagnosticScope.Parser => ParserDiagnostics.ResourceManager.GetString(MessageId)!,
        DiagnosticScope.SymbolResolver => SymbolResolverDiagnostics.ResourceManager.GetString(MessageId)!,

        _ => throw new InvalidOperationException("Invalid diagnostic scope")
    };
}
