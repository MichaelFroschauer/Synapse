using Microsoft.Extensions.Logging;
using SkiaSharp;
using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.GeneticAlgorithm.Mapper;
using Synapse.HITL;
using Synapse.HITL.Scripting.Abstractions;
using Synapse.HITL.Scripting.OpenRouter;
using Synapse.HITL.Scripting.Prompt;
using Synapse.HITL.Scripting.Script;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems;
using Synapse.Problems.JSP;
using Synapse.Problems.TSP;

namespace Synapse.Test;

public class TestGeneticAlgorithm : IMetaheuristicTester
{
    private static IProblem? _problem;
    private static IScriptManager _scriptManager = new ScriptManager();

    private static double[,] ParseCsvData()
    {
        string path = "distance_matrix_20.csv";
        var lines = File.ReadAllLines(path);

        int size = lines.Length - 1;
        double[,] matrix = new double[size, size];
        for (int i = 1; i < lines.Length; i++)
        {
            var parts = lines[i].Split(',');
            for (int j = 1; j < parts.Length; j++)
            {
                matrix[i - 1, j - 1] = double.Parse(parts[j]);
            }
        }

        return matrix;
    }

    class RangeMapper(double inMin, double inMax, double outMin, double outMax)
    {
        public double ScaleFactor => (outMax - outMin) / (inMax - inMin);
        
        public float Map(float value) => (float)Map((double)value);
        public int Map(int value) => (int)Math.Round(Map((double)value));
        
        public double Map(double value)
            => (value - inMin) / (inMax - inMin) * (outMax - outMin) + outMin;
    }
    

    private static void DrawImageOfSolution(ProgressEventArgs? e)
    {
        if (_problem is not TspProblem problem) return;
        if (e?.BestSolution is not TspSolution solution) return;

        Task.Run(() =>
        {
            double maxX = problem.XMaxCoordinate;
            double maxY = problem.YMaxCoordinate;

            int maxImagePixelSize = 2000;
            double padding = 30.0;
            RangeMapper mapper = new RangeMapper(0.0, Math.Max(maxX, maxY), padding, maxImagePixelSize - padding);
            int width = mapper.Map((int)maxX) + (int)padding;
            int height = mapper.Map((int)maxY) + (int)padding;;

            using var bmp = new SKBitmap(width, height);
            using var canvas = new SKCanvas(bmp);

            var cities = solution.GetParametersWithType();
            var paintLine = new SKPaint { Color = SKColors.Blue, StrokeWidth = 2 };
            for (int i = 0; i < cities.Length - 1; i++)
            {
                int cityIndex1 = cities[i];
                var x1 = mapper.Map(problem.Coordinates[cityIndex1].X);
                var y1 = mapper.Map(problem.Coordinates[cityIndex1].Y);

                int cityIndex2 = cities[i + 1];
                var x2 = mapper.Map(problem.Coordinates[cityIndex2].X);
                var y2 = mapper.Map(problem.Coordinates[cityIndex2].Y);
                canvas.DrawLine(x1, y1, x2, y2, paintLine);

                if (i == cities.Length - 2)
                {
                    var startCityIndex = cities[0];
                    var xStart = mapper.Map(problem.Coordinates[startCityIndex].X);
                    var yStart = mapper.Map(problem.Coordinates[startCityIndex].Y);
                    canvas.DrawLine(x2, y2, xStart, yStart, paintLine);
                }
            }
            
            using var paintPoint = new SKPaint();
            paintPoint.Color = SKColors.Red;
            paintPoint.IsAntialias = true;
            
            using var font = new SKFont(SKTypeface.Default, 18);
            using var paintText = new SKPaint();
            paintText.Color = SKColors.Green;
            paintText.IsAntialias = true;

            var pointRadius = 4;
            for (int i = 0; i < problem.Coordinates.Count; i++)
            {
                var coord = problem.Coordinates[i];
                float x = mapper.Map(coord.X);
                float y = mapper.Map(coord.Y);
                
                canvas.DrawCircle(mapper.Map(coord.X), mapper.Map(coord.Y),  pointRadius, paintPoint);
                canvas.DrawText(i.ToString(), x + pointRadius + 5, y, font, paintText);
            }

            using var image = SKImage.FromBitmap(bmp);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var stream = File.OpenWrite($"/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/images//image_{e.Iteration}.png");
            data.SaveTo(stream);
        });
    }

    private static IProblem ParseTspProblem()
    {
        string path =
            "/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/ProblemInstances/TSP/berlin52.tsp";
            //"/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/ProblemInstances/TSP/bier127.tsp";
        IProblemParser parser = new TspProblemParser();
        return parser.Parse(path);
    }
    
    private static IProblem ParseJspProblem()
    {
        string path =
            "/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/ProblemInstances/JSP/abz5.jssp";
        IProblemParser parser = new JspProblemParser();
        return parser.Parse(path);
    }

    private static ILogger<T> GetConsoleLogger<T>()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
        });
        return loggerFactory.CreateLogger<T>();
    }
    
    private static bool _stopped = false;
    private static void HandleGenerationFinished(ProgressEventArgs? e)
    {
        if (e?.AlgorithmController is null || e?.HitlController is null) return;
        
        Console.WriteLine(e.Message);
        // if (e.Iteration == 10 && _stopped == false)
        // {
        //     e.AlgorithmController.RequestPauseAsync();
        //     _stopped = true;
        // }

        if (e.Iteration == 100)
        {
            string code = @"
                async (globals) => {
                    if (globals.Iteration % 10 == 0) 
                    {
                        //globals.HitlController.SetParameter(""temperatureFactor"", 0.9);
                        Console.WriteLine(""Test at %10"");
                    }
                    await Task.CompletedTask;
                }";
            
            string generatedcode = @"
                async (g) => {
                    Parameter[] sol = g.Current.GetParameters();
                    List<int> desiredOrder = new List<int> { 1, 2, 3 };

                    // Check if the first three cities are not 1, 2, 3
                    for (int i = 0; i < 3; i++)
                    {
                        if (!sol[i].Value.Equals(desiredOrder[i]))
                        {
                            // Find the index of the desired city
                            int idx = Array.IndexOf(sol, new Parameter(desiredOrder[i]));
                            if (idx != -1)
                            {
                                // Swap the current city with the desired city
                                var temp = sol[i];
                                sol[i] = sol[idx];
                                sol[idx] = temp;
                            }
                        }
                    }

                    g.Current.SetParameters(sol);
                    await Task.CompletedTask;
                }";
            //e.HitlController.RegisterScriptFromCodeAsync(generatedcode, "test_dynamic_interceptor");
            
            var key = "";
            var userPrompt = "Please create a solution where the first three cities in the tsp are always 1, 2, 3.";
            var client = new OpenRouterClient(key);
            var scriptGenerator = new OpenRouterScriptProvider(client);
            var prompt = ChatPromptFactory.Get(PromptType.Tsp, userPrompt);
            var generatedCode = scriptGenerator.GenerateScriptAsync(prompt).GetAwaiter().GetResult();
            
            Console.WriteLine("-----------------------------------------------------------------------------------------------------");
            Console.WriteLine(generatedCode.Code);
            Console.WriteLine("-----------------------------------------------------------------------------------------------------");
            
            var guid = _scriptManager.CompileAndRegisterAsync(generatedCode.Code!, generatedCode.Name, true).GetAwaiter().GetResult();
            e.HitlController.AddScript(guid);
        }

        if (e.Iteration == 200)
        {
            e.HitlController.RemoveScripts();
        }


        if (e.Iteration == 1000)
        {
            string code = @"
                async (globals) =>
                {
                    if (globals.Iteration == 1001)
                    {
                        var hitlCtrl = globals.HitlController;
                        hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 1, mustComeAfter: 3));
                        hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 3, mustComeAfter: 7));
                        hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 17, mustComeAfter: 10));
                        hitlCtrl.AddConstraint(new TspStartPositionConstraint(start: 1));
                        
                        var edited = new TspSolution(new int[]{ 17,1,12 });
                        hitlCtrl.SetManualEdit(edited, 0.9, new TspPermutationSequenceManualEditApplier());
                    }
                    await Task.CompletedTask;
                }";
            //e.HitlController.RegisterScriptFromCodeAsync(code, "test_dynamic_interceptor");
        }
    }
    
    private static void HandleGenerationFinished2(object? sender, ProgressEventArgs e)
    {
        //Console.WriteLine(e.Message);
        Console.WriteLine($"{e.Iteration} | C Fitness: {e.CurrentBestFitness} | G Fitness: {e.BestFitness} | {e.CurrentBestSolution}");

        if (e.Iteration == 100)
        {
            //var swapEdit = new TspSolution([1,2]);
            //e.HitlController?.AddManualEdit(swapEdit, new Swap2ManualEditApplier(), probability: 1.0, executeForNrOfIterations: 1, "Swap in Permutation");
        }
    }
    
    public static async Task Run()
    {
        var algorithmCtrl = new AlgorithmController();
        //algorithmCtrl.OnProgress += HandleGenerationFinished;
        //algorithmCtrl.OnProgress += DrawImageOfSolution;
        algorithmCtrl.Paused += () => Console.WriteLine("Paused");
        algorithmCtrl.Resumed += () => Console.WriteLine("Resumed");
        
        var hitlCtrl = new HitlController
        {
            AskPreferenceInterval = 1000,
        };
        
        // hitlCtrl.AddSolutionPreference(sol => sol.ToString().Contains("-4-17-"), weight: 2.0);
        
        // hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 1, mustComeAfter: 3));
        // hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 3, mustComeAfter: 7));
        // hitlCtrl.AddConstraint(new TspPrecedenceConstraint(reference: 17, mustComeAfter: 10));
        // hitlCtrl.AddConstraint(new TspStartPositionConstraint(start: 1));
        //hitlCtrl.AddConstraint(new TspClusterConstraint([1,0,31,48,34,35,33,38,36,39,37,47,23,37,39,4,14,5]));
        //hitlCtrl.AddConstraint(new TspFixedEdgeConstraint(mustHaveEdges: [(1,2), (5,6)]));
        
        //hitlCtrl.SetParameter(nameof(HitlParameter.MinPopulationFillSize), 10);
        
        // var edited = new TspSolution(new int[]{ 0,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19 });
        //var edited1 = new TspSolution(new int[]{ 30,20,16,6,1,41,29,22,19,49,28,15,45,24 });
        //hitlCtrl.AddManualEdit(edited1, applier: new TspPermutationSequenceManualEditApplier(), probability: 0.1, executeForNrOfIterations: 1, name: "Set middle section of TSP");

        // var edited2 = new TspSolution(new int[]{ 10,50,32,42,9,8,7,40,18,44,2,17,31,48,35,38,39,37,4,14,5,3,23 });
        // hitlCtrl.AddManualEdit(edited2, applier: new TspPermutationSequenceManualEditApplier(), probability: 0.1, name: Guid.NewGuid().ToString());
        // Optional: pause/unpause algorithm to enforce immediate effect
        // algorithmCtrl.PauseRequested = true;
        // ... perform UI operations ...
        // algorithmCtrl.PauseRequested = false;

        
        // Script registrieren (precompiled delegate)
        // hitlCtrl.AddScript(async globals =>
        // {
        //     // Beispiel: bei Iteration 100 erhöhe MutationRate
        //     if (globals.Iteration == 100)
        //     {
        //         globals.HitlController.SetParameter("mutationRate", 0.2);
        //     }
        //     await Task.CompletedTask;
        // });

        
        // Script aus Text kompilieren (benötigt Roslyn)
        // string code = @"
        // async (globals) => {
        //     if (globals.Iteration % 10 == 0) 
        //     {
        //         //globals.HitlController.SetParameter(""temperatureFactor"", 0.9);
        //         Console.WriteLine(""temperatureFactor"");
        //     }
        //     await Task.CompletedTask;
        // }";
        // await algorithmCtrl.RegisterScriptFromCodeAsync(code, "cooling_adjuster");
        
        double[,] distances = ParseCsvData();
        //var problem = new TspProblem(distances);
        _problem = ParseTspProblem();
        //_problem = ParseJspProblem();


        hitlCtrl.AddSolutionPreference(new TspSolutionSimilarity(), new TspSolution(new int[] { 1, 2, 3, 4 }));
        
        var cfg = new GeneticAlgorithmConfig {
            PopulationSize = 700,
            MaxIterations = 500,
            CrossoverProbability = 0.95,
            MutationProbability = 0.05,
            AlgorithmController = algorithmCtrl,
            HitlController = hitlCtrl,
            ProgressInterval = 1
        };
        
        MapperBootstrap.RegisterAllMappings();
        var logger = GetConsoleLogger<GeneticAlgorithm>();
        IMetaheuristic ga = new GeneticAlgorithm(cfg, logger);
        ga.ProgressChanged += HandleGenerationFinished2;
        
        var ct = new CancellationTokenSource();
        // ct.CancelAfter(1000);
        
        // hitlCtrl.AskPreference = (IEnumerable<ISolution> solutions1, IEnumerable<ISolution> solutions2) =>
        // {
        //     var s1 = solutions1.First();
        //     var s2 = solutions2.First();
        //     Console.WriteLine($"Fitness S1: {s1.Fitness}, Solution: {s1}");
        //     Console.WriteLine($"Fitness S2: {s2.Fitness}, Solution: {s2}");
        //     Console.WriteLine("What solution would you like to choose? (1, 2)");
        //     var userSelect = Console.ReadLine();
        //     return userSelect == "2" ? 2 : 1;
        // };
        
        var bestTour = (await ga.SolveAsync(_problem, ct.Token)) as TspSolution;
        Console.WriteLine(Math.Abs(bestTour?.Fitness ?? 0.0) + "\t" + bestTour);
    }
}
