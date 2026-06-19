using System.Reflection;
using Synapse.HITL.Problems.TSP;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems.TSP;
using Synapse.Reflection;
using Synapse.Reflection.ClassLoader;
using Synapse.Reflection.Descriptors;
using Synapse.Reflection.Models;

namespace Synapse.Test;

public static class ReflectionHelper
{
    public static IEnumerable<Type?> FindTypes(Type typeName)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { return ex.Types.Where(t => t != null); }
            })
            .Where(t => t != null
                        && typeName.IsAssignableFrom(t)
                        && t.IsClass
                        && !t.IsAbstract);
    }

    public static void PrintProperties(Type t)
    {
        Console.WriteLine($"Typ: {t.FullName}");
        var props = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        if (!props.Any())
        {
            Console.WriteLine("  (keine öffentlichen Instanz-ConstructorParameters)");
            return;
        }
        foreach (var p in props)
        {
            Console.WriteLine($"  Property: {p.Name} ({p.PropertyType.Name}) - CanRead: {p.CanRead}, CanWrite: {p.CanWrite}");
        }
    }
    
    public static object CreateInstanceWithDefaults(Type type)
    {
        // 1) User parameterless constructor
        var parameterlessCtor = type.GetConstructor(Type.EmptyTypes);
        if (parameterlessCtor != null)
            return Activator.CreateInstance(type);

        // 2) Suche Konstruktor mit der kleinsten Parameteranzahl
        var ctors = type.GetConstructors();
        if (!ctors.Any())
            throw new InvalidOperationException($"Kein öffentlicher Konstruktor für {type.FullName} gefunden.");

        var ctor = ctors.OrderBy(c => c.GetParameters().Length).First();
        var parameters = ctor.GetParameters();
        var args = new object[parameters.Length];

        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];

            // Wenn Parameter einen Default-Wert hat, benutze ihn
            if (p.HasDefaultValue)
            {
                args[i] = p.DefaultValue;
                continue;
            }

            // Sonst bestimme einen "vernünftigen" Default-Wert
            args[i] = GetDefaultValueForType(p.ParameterType);
        }

        return ctor.Invoke(args);
    }

    private static object GetDefaultValueForType(Type t)
    {
        if (!t.IsValueType) return null; // Nullable<T> -> null
        if (Nullable.GetUnderlyingType(t) != null) return null;

        // special treatment for Guid
        if (t == typeof(Guid)) return Guid.Empty;

        // Für Enums -> 0 cast
        if (t.IsEnum) return Activator.CreateInstance(t);

        // for values types (int, double, struct...) -> Activator.CreateProblem
        return Activator.CreateInstance(t);
    }
    
    private static bool _assembliesSearched = false;
    private static List<Assembly> _assemblies = new();
    public static IEnumerable<Assembly> ReferencedAssembliesToScan()
    {
        if (_assembliesSearched) return _assemblies;
        _assembliesSearched = true;
        var referencedAssemblies = Assembly.GetExecutingAssembly().GetReferencedAssemblies();
        foreach (var name in referencedAssemblies)
        {
            var asm = Assembly.Load(name);
            _assemblies.Add(asm);
        }

        return _assemblies;
    }
}

public class ProblemFactory()
{
    
}

public class TestReflectionFinder : IMetaheuristicTester
{
    public static Task Run()
    {
        var reg = new ReflectionRegistry(() => ReflectionHelper.ReferencedAssembliesToScan());
        var constraints = reg.GetAssignableTypes(typeof(IConstraint)).ToList();
        var constraints2 = reg.GetAssignableTypes<IConstraint>().ToList();
        var constraints3 = reg.GetAssignableTypes(constraints2[0].GetType()).ToList();

        var algorithmLoader = new AlgorithmLoader(reg);
        var algorithmDescriptor = algorithmLoader.Load();

        var userSettableConstraints = constraints.BuildUserSettableClasses();
        var applicable = userSettableConstraints.ApplicableToSolution(typeof(TspSolution)).ToList();
        
        var parserLoader = new ProblemParserLoader(reg);
        var problemInstanceLoader = new ProblemInstanceLoader(reg, parserLoader);

        var problemLoader = new ProblemLoader(reg, problemInstanceLoader);
        var problemDescriptors = problemLoader.Load();
        
        var tspProblemDescriptor = problemDescriptors.FirstOrDefault(p => p.ProblemType == ProblemType.Tsp);
        var bier127InstanceDescriptor = tspProblemDescriptor?.ProblemInstances.FirstOrDefault(i => i.Name == "bier127");
        var bier127Problem = bier127InstanceDescriptor?.CreateProblem();


        var funcProblemDescriptor = problemDescriptors.FirstOrDefault(p => p.ProblemType == ProblemType.Function);
        var rastriginInstanceDescriptor = funcProblemDescriptor?.ProblemInstances.FirstOrDefault(i => i.Name == "Rastrigin Function Problem");
        var ctorParam = rastriginInstanceDescriptor?.SettableClass?.ConstructorParameters.First();
        Console.WriteLine((ctorParam as UserSettableProperty)!.DisplayName);
        ctorParam.Value = 10;
        var rastriginInstance = rastriginInstanceDescriptor?.SettableClass?.GetInstance();
        var rastriginProblem = rastriginInstanceDescriptor?.CreateProblem();

        IDescriptorRegistry descriptorRegistry = new DescriptorRegistry(reg);
        var algorithmDescriptors = descriptorRegistry.Get<AlgorithmDescriptor>().ToList();

        // TEST
        foreach (var userSettableClass in applicable)
        {
            if (userSettableClass.Name == "TspPrecedenceConstraint")
            {
                foreach (var p in userSettableClass.ConstructorParameters)
                {
                    if (p.UserSettable)
                    {
                        Console.WriteLine($"Set value for property {p.Type.Name} {p.Name}: ");
                        var userInput = Console.ReadLine();

                        p.Value = p.Type switch
                        {
                            { } t when t == typeof(int) => int.TryParse(userInput, out var i) ? i : null,
                            { } t when t == typeof(long) => long.TryParse(userInput, out var i) ? i : null,
                            { } t when t == typeof(double) => double.TryParse(userInput, out var d) ? d : null,
                            { } t when t == typeof(bool) => bool.TryParse(userInput, out var b) ? b : null,
                            { } t when t == typeof(string) => userInput,
                            { } t when t == typeof(IEnumerable<int>) => userInput?.Split(',').Select(int.Parse),
                            _ => null
                        };
                    }
                    else
                    {
                        p.Value = p.Type switch
                        {
                            { } t when t == typeof(IProblem) => new TspProblem(new double[,] { }),
                            _ => null
                        };
                        Console.WriteLine($"Not provided by the user: {p.Type.Name} {p.Name}");
                    }
                }
                
                var tspConstraint = userSettableClass.GetInstance() as TspPrecedenceConstraint;
                if (tspConstraint != null)
                {
                    Console.WriteLine(tspConstraint.ToString());
                }
            }
            
            Console.WriteLine(userSettableClass);
            Console.WriteLine($"Constructor contains IProblem: {userSettableClass.ConstructorContainsDerivedType(typeof(IProblem))}");
        }
        
        // var constraintTypes = ReflectionService.Constraints.For(TSP);
        // foreach (var type in constraintTypes)
        // {
        //     var properties = type.GetProperties();
        //     foreach (var property in properties)
        //     {
        //         var pType = property.Type;
        //         var pName = property.Name;
        //     }
        //     
        //     ConstructorInfo[] constructors = type.GetConstructors();
        //
        //     foreach (var ctor in constructors)
        //     {
        //         Console.WriteLine($"Constructor: {ctor}");
        //     }
        //     
        //     var ctor2 = type.GetConstructor(new[] { typeof(string), typeof(int) });
        //     object person = ctor2.Invoke(new object[] { "John", 25 });
        //     
        //     //IConstraint c = type.Instantiate();
        // }
        
        
        var solutionSimilarityClasses = reg.GetAssignableTypes(typeof(ISolutionSimilarity))
            .BuildUserSettableClasses().ApplicableToSolution(typeof(TspSolution)).ToList();

        if (solutionSimilarityClasses.First().TryGetInstance<ISolutionSimilarity>(out var solutionSimilarity, out var error))
        {
            Console.WriteLine(solutionSimilarity);
        }
        else
        {
            Console.WriteLine(error);    
        }

        return Task.CompletedTask;
    }
}
