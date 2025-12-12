using System.Collections.Generic;
using System.Linq;

using Torque.Compiler.Diagnostics;


namespace Torque.Compiler;




public class ControlFlowAnalysisReporter(IReadOnlyList<ControlFlowGraph> graphs) : DiagnosticReporter<Diagnostic.ControlFlowAnalyzerCatalog>
{
    public IReadOnlyList<ControlFlowGraph> Graphs { get; } = graphs;




    public void Report()
    {
        foreach (var graph in Graphs)
        {
            var lastBlock = graph.Blocks.Last();
            var lastStatement = lastBlock.Statements.Last();
            var firstStatement = lastBlock.Statements.First();

            // TODO: finish this
        }
    }
}
