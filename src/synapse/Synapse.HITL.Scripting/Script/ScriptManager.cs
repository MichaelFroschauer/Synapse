using System.Collections.Concurrent;
using System.Reflection;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Synapse.HITL.Scripting.Abstractions;
using Synapse.OptimizationCore.Common;

namespace Synapse.HITL.Scripting.Script;

// Lightweight script manager. Uses Microsoft.CodeAnalysis.CSharp.Scripting when available.
public class ScriptManager : IScriptManager
{
    private readonly List<Assembly> _scriptAssemblies;
    private readonly List<string> _scriptImports;
    private readonly ConcurrentDictionary<Guid, OptimizationCore.HITL.Script> _scripts = new();
    
    public ScriptManager()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var scriptImports = assemblies
            .SelectMany(a => a.GetTypes())
            //.Where(t => t.GetCustomAttributes(typeof(ScriptImportAttribute), false).Any())
            .ToList();

        _scriptAssemblies = scriptImports
            .Select(t => t.Assembly)
            .Distinct()
            .ToList();

        _scriptImports = scriptImports.Select(s => s.Namespace)
            .Where(s => !string.IsNullOrEmpty(s))
            .Distinct()
            .ToList()!;
    }

    public Guid Register(Func<ScriptGlobals, Task> func, string? name = null, bool executeOnce = false)
    {
        var scriptId = Guid.NewGuid();
        var script = new OptimizationCore.HITL.Script
        {
            Id = scriptId,
            Name = string.IsNullOrWhiteSpace(name) ? scriptId.ToString() : name,
            ScriptText = $"No {nameof(OptimizationCore.HITL.Script.ScriptText)} because it is pre-compiled code.",
            CompiledScript = func,
            ExecuteOnce = executeOnce
        };
        _scripts.AddOrUpdate(script.Id, script, (key, value) => script);
        return scriptId;
    }

    public void Unregister(Guid id) => _scripts.TryRemove(id, out _);

    public async Task<Guid> CompileAndRegisterAsync(string code, string? name = null, bool executeOnce = false)
    {
        // This method requires Microsoft.CodeAnalysis.CSharp.Scripting
        // If you don't want the package, use Register with a precompiled delegate.
        try
        {
            var options = ScriptOptions.Default
                .WithReferences(_scriptAssemblies)
                .WithImports(_scriptImports);

            // Evaluate the code *as a Func<ScriptGlobals, Task>* asynchronously. The code string must be an expression
            var func = await Task.Run(async () => await CSharpScript.EvaluateAsync<Func<ScriptGlobals, Task>>(code, options)
                .ConfigureAwait(false)).ConfigureAwait(false);
            if (func == null) throw new InvalidOperationException("Compiled script returned null delegate.");
            
            var scriptId = Guid.NewGuid();
            var script = new OptimizationCore.HITL.Script
            {
                Id = scriptId,
                Name = string.IsNullOrWhiteSpace(name) ? scriptId.ToString() : name,
                ScriptText = code,
                CompiledScript = func,
                ExecuteOnce = executeOnce
            };
            _scripts.AddOrUpdate(script.Id, script, (key, value) => script);
            
            return scriptId;
        }
        catch (CompilationErrorException ex)
        {
            throw new CompilationErrorException($"Failed to compile script.\n{ex.Message}", ex.Diagnostics);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to compile script. Ensure Microsoft.CodeAnalysis.CSharp.Scripting is referenced and the script returns a Func<ScriptGlobals, Task>. Exception: ", ex);
        }
    }
    
    public async Task ExecuteAllAsync(ScriptGlobals globals)
    {
        foreach (var script in _scripts.Values)
        {
            await script.CompiledScript(globals).ConfigureAwait(false);
        }
    }
    
    public async Task ExecuteSetAsync(ScriptGlobals globals, IEnumerable<Guid> scriptIds)
    {
        foreach (var scriptId in scriptIds)
        {
            await _scripts[scriptId].CompiledScript(globals).ConfigureAwait(false);
        }
    }

    public IEnumerable<OptimizationCore.HITL.Script> GetRegisteredScripts() => _scripts.Values;
    public OptimizationCore.HITL.Script? GetRegisteredScript(Guid scriptId) => _scripts[scriptId];

    public IEnumerable<OptimizationCore.HITL.Script> GetRegisteredScripts(IEnumerable<Guid> scriptIds)
        => _scripts.Values.Where(s => scriptIds.Contains(s.Id));

    public bool ScriptExists(Guid id) => _scripts.ContainsKey(id);

    public bool ScriptsExist() => !_scripts.IsEmpty;
}
