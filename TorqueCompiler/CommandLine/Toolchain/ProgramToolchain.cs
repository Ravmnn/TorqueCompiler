using System.IO;
using System.Collections.Generic;
using Torque.Compiler;


namespace Torque.CommandLine.Toolchain;




public static class ProgramToolchain
{
    public static void Compile(string bitCode, string outputFile, CompilerOptions options)
    {
        TempFiles.ForTempFileDo(file =>
        {
            File.WriteAllText(file, bitCode);

            var compiler = NewCompilerProgram(file, outputFile, options);
            compiler.Run();
        });
    }


    private static CompilerProgram NewCompilerProgram(string inputFile, string outputFile, CompilerOptions options)
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
