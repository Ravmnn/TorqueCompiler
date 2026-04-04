using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using Torque.CommandLine;
using Torque.Compiler.Tokens;
using Torque.Compiler.Diagnostics.Catalogs;
using Torque.Compiler.Diagnostics.Catalogs.Resources;


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
    Binder,
    TypeChecker,
    ControlFlowAnalyzer
}




public readonly struct Diagnostic()
{
    public required SourceCode SourceCode { get; init; }

    public required int Code { get; init; }
    public required DiagnosticScope Scope { get; init; }
    public required DiagnosticSeverity Severity { get; init; }

    public required string MessageId { get; init; }


    public IReadOnlyList<object> Arguments { get; init; } = [];
    public Span? Location { get; init; }




    public static Diagnostic FromCatalog<T>(SourceCode source, int code, IReadOnlyList<object>? arguments = null, Span? location = null)
        where T : Enum
    {
        var (item, scope, severity) = GetFromCatalog<T>(code);

        return new Diagnostic
        {
            SourceCode = source,
            Code = code,
            Scope = scope,
            Severity = severity,
            MessageId = item.ToString(),
            Arguments = arguments ?? [],
            Location = location
        };
    }


    private static (T, DiagnosticScope, DiagnosticSeverity) GetFromCatalog<T>(int code)
        where T : Enum
    {
        var enumType = typeof(T);
        var item = (T)Enum.ToObject(enumType, code);

        var name = Enum.GetName(enumType, item);
        var field = enumType.GetField(name!, BindingFlags.Public | BindingFlags.Static);
        var attribute = field!.GetCustomAttribute<ItemAttribute>()!;

        return (item, attribute.Scope, attribute.Severity);
    }




    public string GetMessage() => Scope switch
    {
        DiagnosticScope.Lexer => LexerDiagnostics.ResourceManager.GetString(MessageId)!,
        DiagnosticScope.Parser => ParserDiagnostics.ResourceManager.GetString(MessageId)!,
        DiagnosticScope.Binder => BinderDiagnostics.ResourceManager.GetString(MessageId)!,
        DiagnosticScope.TypeChecker => TypeCheckerDiagnostics.ResourceManager.GetString(MessageId)!,
        DiagnosticScope.ControlFlowAnalyzer => ControlFlowAnalyzerDiagnostics.ResourceManager.GetString(MessageId)!,

        _ => throw new InvalidOperationException("Invalid diagnostic scope")
    };


    public string GetFormattedMessage()
        => string.Format(GetMessage(), Arguments.ToArray());
}
