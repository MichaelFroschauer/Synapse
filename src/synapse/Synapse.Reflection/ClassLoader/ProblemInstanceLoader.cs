using System.Reflection;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.Problems;
using Synapse.Problems.Function;
using Synapse.Reflection.Descriptors;

namespace Synapse.Reflection.ClassLoader;

public class ProblemInstanceLoader : BaseClassLoader
{
    private readonly ITypeRegistry _registry;
    private readonly ProblemParserLoader _parserLoader;
    private readonly string _problemInstanceBasePath;

    public ProblemInstanceLoader(
        ITypeRegistry registry,
        ProblemParserLoader parserLoader,
        string? problemInstanceBasePath = null)
    {
        _registry = registry;
        _parserLoader = parserLoader;
        _problemInstanceBasePath = ResolveProblemInstancesBasePath(problemInstanceBasePath);
        WarnIfProblemInstancesPathMissing(_problemInstanceBasePath);
    }

    public override IReadOnlyList<ProblemInstanceDescriptor> Load()
    {
        var list = new List<ProblemInstanceDescriptor>();
        
        // Code Problem Instances
        var types = _registry.GetAssignableTypes<IFunctionProblemInstance>();
        foreach (var t in types)
            if (TryBuild(t, out var d))
                list.Add(d!);

        // File Problem Instances
        var problemInstances = DiscoverProblemInstances(_problemInstanceBasePath);
        var problemParsers = _parserLoader.Load().Select(p => p as ProblemParserDescriptor).ToList();
        
        foreach (var t in problemInstances)
        {
            var problemParserType = problemParsers.FirstOrDefault(p => p.ProblemType == t.ProblemType);
            if (problemParserType is null) throw new NotImplementedException($"Problem parser for problem type {t.ProblemType} not found");
            var problemParser = Activator.CreateInstance(problemParserType.ImplementationType) as IProblemParser;
            if (problemParser is null) throw new NotImplementedException($"Problem parser empty constructor for problem type {t.ProblemType} not found");
            
            var descriptor = new ProblemInstanceDescriptor(problemParser, t.Name, t.Path, t.ProblemType);
            list.Add(descriptor);
        }

        return list;
    }
    
    private bool TryBuild(Type t, out ProblemInstanceDescriptor? descriptor) =>
        TryBuild<ProblemInstanceDescriptor, IFunctionProblemInstance, ProblemInstanceAttribute>(
            t, (typ, attr) => new ProblemInstanceDescriptor(typeof(FunctionProblem), typ, attr), out descriptor
        );

    private static List<FileProblemInstances> DiscoverProblemInstances(string basePath)
    {
        var problemInstances = new List<FileProblemInstances>();
        if (!Directory.Exists(basePath)) return problemInstances;
        
        
        var problemTypes = Enum.GetValues<ProblemType>();
        foreach (var type in problemTypes)
        {
            var typeString = type.ToString().ToUpper();
            var folder = Path.Combine(basePath, typeString);
            if (!Directory.Exists(folder)) continue;
            
            string[] extensions = type switch
            {
                ProblemType.Tsp => new[] { ".tsp" },
                ProblemType.Qap => new[] { ".dat" },
                ProblemType.Jsp => new[] { ".jssp" },
                _ => throw new NotImplementedException("Unknown problem type could not find any problem instance"),
            };
            
            problemInstances.AddRange(Directory.EnumerateFiles(folder)
                .Where(f => extensions.Contains(Path.GetExtension(f), StringComparer.OrdinalIgnoreCase))
                .Select(f => new FileProblemInstances
                {
                    Name = Path.GetFileNameWithoutExtension(f),
                    Path = f,
                    ProblemType = type
                })
                .ToList());
        }
        
        return problemInstances;
    }
    
    
    private struct FileProblemInstances
    {
        public string Name { get; init; }
        public string Path { get; init; }
        public ProblemType ProblemType { get; init; }

        public FileProblemInstances(string filePath)
        {
            Name = System.IO.Path.GetFileNameWithoutExtension(filePath);
            Path = filePath;
            ProblemType = GetProblemType(filePath);
        }
    
        private static ProblemType GetProblemType(string problemInstancePath)
        {
            var info = new FileInfo(problemInstancePath);
            var directoryName = info.Directory?.Name;

            return directoryName?.ToUpper() switch
            {
                "TSP" => ProblemType.Tsp,
                "QAP" => ProblemType.Qap,
                "JSP" => ProblemType.Jsp,
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }

    private static string ResolveProblemInstancesBasePath(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            return Path.GetFullPath(configuredPath);

        var envPath = Environment.GetEnvironmentVariable("SYNAPSE_PROBLEM_INSTANCES_PATH");
        if (!string.IsNullOrWhiteSpace(envPath))
            return Path.GetFullPath(envPath);

        // Walk upwards from the app base directory until we find the repo folder containing ProblemInstances.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "ProblemInstances");
            if (Directory.Exists(candidate))
                return candidate;

            current = current.Parent;
        }

        // Last fallback for local dev/test runs started from repo root.
        return Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "ProblemInstances"));
    }

    private static void WarnIfProblemInstancesPathMissing(string basePath)
    {
        if (Directory.Exists(basePath))
            return;

        Console.Error.WriteLine(
            $"[Synapse][Warning] ProblemInstances path not found: '{basePath}'. " +
            "Only code-based problem instances will be available. " +
            "Set SYNAPSE_PROBLEM_INSTANCES_PATH or provide a base path explicitly.");
    }
}
