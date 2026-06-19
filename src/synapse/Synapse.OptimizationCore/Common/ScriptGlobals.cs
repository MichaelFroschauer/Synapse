using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.OptimizationCore.Random;

namespace Synapse.OptimizationCore.Common;

// Scripting support: register a script delegate or compile a string into a script (optional package required)
public class ScriptGlobals
{
    public IProblem? Problem { get; init; }
    public IFitnessEvaluator? Evaluator { get; init; }
    public IAlgorithmController? AlgorithmController { get; init; }
    public IHitlController? HitlController { get; init; }
    public ISolution? Current { get; set; }
    public ISolution? Best { get; init; }
    public int Iteration { get; init; }
    public IRandom? Random { get; init; }
    //public ProgressEventArgs Progress { get; set; }
}