using System.Collections.Generic;

using Torque.Compiler.Diagnostics;
using Torque.Compiler.Diagnostics.Catalogs;
using Torque.Compiler.Tokens;


namespace Torque.Compiler;




public sealed class TorqueLexerReporter(TorqueLexer lexer) : DiagnosticReporter<LexerCatalog>
{
    public TorqueLexer Lexer { get; } = lexer;




    public override Diagnostic Report(LexerCatalog item, IReadOnlyList<object>? arguments = null, Span? location = null)
        => base.Report(item, arguments, location ?? Lexer.GetCurrentLocation());


    public bool ReportMultilineCommentDiagnostics(Span commentStart)
    {
        if (!Lexer.Iterator.AtEnd())
            return false;

        Report(LexerCatalog.UnclosedMultilineComment, location: commentStart);
        return true;
    }


    public void ReportStringErrors(Span quoteLocation)
    {
        if (Lexer.Iterator.AtEnd())
            Report(LexerCatalog.UnclosedString, location: quoteLocation);
    }


    public void ReportCharErrors(IReadOnlyList<byte> data, Span quoteLocation)
    {
        if (data.Count == 0)
            Report(LexerCatalog.SingleCharacterEmpty);

        if (Lexer.Iterator.AtEnd())
            Report(LexerCatalog.UnclosedSingleCharacterString, location: quoteLocation);

        else if (data.Count > 1)
            Report(LexerCatalog.SingleCharacterMoreThanOne);
    }
}
