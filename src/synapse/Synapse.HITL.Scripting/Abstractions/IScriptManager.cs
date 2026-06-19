using Synapse.OptimizationCore.Common;

namespace Synapse.HITL.Scripting.Abstractions;

public interface IScriptManager
{
    Task<Guid> CompileAndRegisterAsync(string code, string? name = null, bool executeOnce = false);
    Guid Register(Func<ScriptGlobals, Task> func, string? name = null, bool executeOnce = false);
    void Unregister(Guid id);
    Task ExecuteAllAsync(ScriptGlobals globals);
    IEnumerable<OptimizationCore.HITL.Script> GetRegisteredScripts();
    OptimizationCore.HITL.Script? GetRegisteredScript(Guid scriptId);
    IEnumerable<OptimizationCore.HITL.Script> GetRegisteredScripts(IEnumerable<Guid> scriptIds);
    bool ScriptExists(Guid id);
    bool ScriptsExist();
    Task ExecuteSetAsync(ScriptGlobals globals, IEnumerable<Guid> scriptIds);
}
