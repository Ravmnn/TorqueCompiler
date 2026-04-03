using System.IO;
using System.Collections.Generic;

using Torque.Compiler.CodeGen;


namespace Torque.CommandLine.Toolchain;




public static class ProgramToolchain
{
    public static void Compile(string bitCode, string outputFile, IRGenerationOptions options)
    {
        var programOptions = CompilerProgramOptions.FromCompilerOptions(options);
        Compile(bitCode, outputFile, programOptions);
    }


    public static void Compile(string bitCode, string outputFile, CompilerProgramOptions options)
    {
        TempFiles.ForTempFileDo(file =>
        {
            File.WriteAllText(file, bitCode);

            var compiler = NewCompilerProgram(file, outputFile, options);
            compiler.Run();
        });
    }


    private static CompilerProgram NewCompilerProgram(string inputFile, string outputFile, CompilerProgramOptions options)
        => new CompilerProgram
        {
            InputFile = inputFile,
            OutputFile = outputFile,
            Options = options
        };




    public static void Link(IReadOnlyList<string> inputFiles, string outputFile, LinkerProgramOptions options)
    {
        var linker = NewLinkerProgram(inputFiles, outputFile, options);
        linker.Run();
    }


    private static LinkerProgram NewLinkerProgram(IReadOnlyList<string> inputFiles, string outputFile, LinkerProgramOptions options)
        => new LinkerProgram
        {
            InputFiles = inputFiles,
            OutputFile = outputFile,
            Options = options
        };
}
