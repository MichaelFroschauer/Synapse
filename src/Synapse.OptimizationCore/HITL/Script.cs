using Synapse.OptimizationCore.Common;

namespace Synapse.OptimizationCore.HITL;

public class Script
{
    public required Guid Id { get; init; }
    public required string Name { get; set; }
    public required Func<ScriptGlobals, Task> CompiledScript { get; set; }
    public required string ScriptText { get; set; }
    public bool ExecuteOnce { get; set; }
}

public class ScriptEntry
{
    public required Script Script { get; init; }
    public bool Executed { get; set; } = false;
    public int ExecutionCount { get; set; } = 0;
    public bool ExecutionPossible => !Script.ExecuteOnce || !Executed;
}
