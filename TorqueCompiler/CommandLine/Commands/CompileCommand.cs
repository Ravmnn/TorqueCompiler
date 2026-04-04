#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.


using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Threading;

using Spectre.Console;
using Spectre.Console.Cli;

using Torque.CommandLine.Exceptions;
using Torque.CommandLine.Toolchain;
using Torque.Compiler.AST.Statements;
using Torque.Compiler.CodeGen;
using Torque.Compiler.Parsing;
using Torque.Compiler.Target;


namespace Torque.CommandLine.Commands;




public class CompileCommandSettings : CommandSettings
{
    [CommandArgument(0, "<file>")]
    public FileInfo File { get; init; }




    [CommandOption("-O|--output-directory")]
    [Description("The output folder to create the object files")]
    public DirectoryInfo? OutputDirectory { get; init; }




    [CommandOption("--target-arch")]
    [Description("The CPU architecture to generate instructions")]
    [DefaultValue(ArchitectureType.X86_64)]
    public ArchitectureType Architecture { get; init; }


    [CommandOption("--target-os")]
    [Description("The OS target type")]
    [DefaultValue(OperationalSystemType.Linux)]
    public OperationalSystemType OperationalSystem { get; init; }


    [CommandOption("--target-environment")]
    [Description("The environment target type")]
    [DefaultValue(EnvironmentType.GNU)]
    public EnvironmentType Environment { get; init; }


    [CommandOption("--target-vendor")]
    [Description("The vendor target type")]
    [DefaultValue(VendorType.PC)]
    public VendorType Vendor { get; init; }




    [CommandOption("-d|--debug")]
    [Description("Generate debug information")]
    public bool Debug { get; init; }


    [CommandOption("--pic")]
    [Description("Generate Position Independent Code")]
    [DefaultValue(true)]
    public bool PIC { get; init; }




    [CommandOption("--print-ast")]
    [Description("Print the generated AST")]
    public bool PrintAST { get; init; }


    [CommandOption("--print-llvm")]
    [Description("Print the generated LLVM IR")]
    public bool PrintLLVM { get; init; }


    [CommandOption("--print-asm")]
    [Description("Print the generated assembly code")]
    public bool PrintASM { get; init; }




    public override ValidationResult Validate()
    {
        if (!File.Exists)
            return ValidationResult.Error($"Could not open source file \"{File.Name}\"");

        if (OutputDirectory is not null && !OutputDirectory!.Exists)
            Directory.CreateDirectory(OutputDirectory.FullName);

        return ValidationResult.Success();
    }
}




public class CompileCommand : Command<CompileCommandSettings>
{
    protected override int Execute(CommandContext context, CompileCommandSettings settings, CancellationToken cancellationToken)
    {
        Torque.Initialize(settings);

        try
        {
            ExecuteBasedOnSettings(settings);
        }
        catch (InterruptCompileException) {}
        catch (Exception exception)
        {
            // Spectre.Console hides the stack trace when displaying exceptions,
            // so we catch it here and print it ourselves to avoid that.
            DiagnosticLogger.LogInternalError(exception);
        }

        return 0;
    }


    private static void ExecuteBasedOnSettings(CompileCommandSettings settings)
    {
        if (settings.PrintLLVM || settings.PrintASM || settings.PrintAST)
            PrintRequestedModuleFormats(settings);
        else
            Torque.Compile(settings);
    }




    private static void PrintRequestedModuleFormats(CompileCommandSettings settings)
    {
        var statements = CompilerSteps.BuildFinalAST(SourceCode.Source!);

        if (settings.PrintAST)
        {
            PrintAST(statements);
            return;
        }

        var module = CompilerSteps.SemanticAnalysis(statements, SourceCode.FilePath!);
        var options = settings.ToIRGenerationOptions() with { CompileImportedModules = false };
        var bitCode = CompilerSteps.Compile(module, options);

        if (settings.PrintLLVM)
            PrintLLVM(bitCode);

        else if (settings.PrintASM)
            PrintASM(settings, bitCode);
    }


    private static void PrintAST(IReadOnlyList<Statement> statements)
    {
        Console.WriteLine(new ASTPrinter().Print(statements));
    }


    private static void PrintLLVM(string bitCode)
    {
        Console.WriteLine(bitCode);
    }


    private static void PrintASM(CompileCommandSettings settings, string bitCode)
    {
        var assembly = CompileBitCodeToAssembly(settings, bitCode);
        Console.WriteLine(assembly);
    }


    private static string CompileBitCodeToAssembly(CompileCommandSettings settings, string bitCode) => TempFiles.ForTempFileDo(file =>
    {
        var options = CompilerProgramOptions.FromCompileCommandSettings(settings)
            with { OutputType = OutputType.Assembly };

        ProgramToolchain.Compile(bitCode, file, options);
        return File.ReadAllText(file);
    });
}




public static class CompileCommandSettingsExtensions
{
    public static IRGenerationOptions ToIRGenerationOptions(this CompileCommandSettings settings)
        => new IRGenerationOptions
        {
            OutputDirectory = settings.OutputDirectory,
            Debug = settings.Debug,
            PIC = settings.PIC
        };
}