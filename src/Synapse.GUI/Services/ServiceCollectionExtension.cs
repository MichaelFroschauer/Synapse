using System;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Synapse.Algorithms.GeneticAlgorithm;
using Synapse.Algorithms.NSGA_II;
using Synapse.Algorithms.ParticleSwarm;
using Synapse.Algorithms.RAPGA;
using Synapse.GUI.Models;
using Synapse.GUI.ViewModels;
using Synapse.GUI.ViewModels.Controls;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig;
using Synapse.GUI.ViewModels.Controls.AlgorithmResults;
using Synapse.GUI.ViewModels.Pages;
using Synapse.HITL;
using Synapse.HITL.Scripting.Abstractions;
using Synapse.HITL.Scripting.OpenRouter;
using Synapse.HITL.Scripting.Script;
using Synapse.JobManagement;
using Synapse.JobManagement.Persistence;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Common.Impl;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;
using AlgorithmExecutionControlViewModel = Synapse.GUI.ViewModels.Controls.AlgorithmExecutionControlViewModel;
using AlgorithmExecutionStateCardViewModel = Synapse.GUI.ViewModels.Controls.AlgorithmExecutionStateCardViewModel;
using AlgorithmSelectionViewModel = Synapse.GUI.ViewModels.Controls.AlgorithmSelectionViewModel;
using SidebarLeftViewModel = Synapse.GUI.ViewModels.Controls.SidebarLeftViewModel;

namespace Synapse.GUI.Services;

public static class ServiceCollectionExtension
{
    public static void AddCommonServices(this IServiceCollection services)
    {
        // Controller
        services.AddTransient<IAlgorithmController, AlgorithmController>();
        services.AddTransient<IHitlController, HitlController>();

        // Algorithms
        services.AddTransient<GeneticAlgorithm>();
        services.AddTransient<Nsga2Algorithm>();
        services.AddTransient<ParticleSwarmAlgorithm>();
        services.AddTransient<RAPGA>();

        // JobManagement
        services.AddSingleton<IJobSnapshotStore>(sp =>
        {
            var path = Environment.GetEnvironmentVariable("SNAPSHOT_DIR_PATH") ?? "";
            if (String.IsNullOrWhiteSpace(path))
            {
                path = Path.Combine(Environment.CurrentDirectory, "JobSnapshots");
            }
            return new FileJobSnapshotStore(path);
        });
        services.AddSingleton<IJobManager, JobManager>();

        // Windows + User Controls
        services.AddTransient<AlgorithmExecutionControlViewModel>();
        services.AddTransient<AlgorithmExecutionStateCardViewModel>();
        services.AddTransient<GaAlgorithmConfigViewModel>();
        services.AddTransient<Nsga2AlgorithmConfigViewModel>();
        services.AddTransient<PsoAlgorithmConfigViewModel>();
        services.AddTransient<RapGaAlgorithmConfigViewModel>();
        services.AddTransient<HitlParametersViewModel>();
        services.AddTransient<InteractionTriggersViewModel>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<BottomBarViewModel>();
        services.AddTransient<SidebarLeftViewModel>();
        services.AddTransient<SidebarRightViewModel>();
        services.AddTransient<AlgorithmSelectionViewModel>();
        services.AddTransient<RunningJobsViewModel>();
        services.AddTransient<SolutionPreferenceSelectorViewModel>();

        services.AddTransient<FunctionResultViewModel>();
        services.AddTransient<TspResultViewModel>();
        services.AddTransient<QapResultViewModel>();
        services.AddTransient<JspResultViewModel>();

        // Pages
        services.AddTransient<AlgorithmConfiguratorPageViewModel>();
        services.AddTransient<HitlConfiguratorPageViewModel>();
        services.AddTransient<HitlScriptingPageViewModel>();
        services.AddTransient<ResultsPageViewModel>();
        services.AddTransient<AiConfiguratorPageViewModel>();

        // Page Navigation
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IAlgorithmConfigViewModelService, AlgorithmConfigViewModelService>();
        services.AddSingleton<IProblemResultViewModelService, ProblemResultViewModelService>();

        // Factories
        services.AddSingleton<IPageFactory, PageFactory>();
        services.AddSingleton<Func<ApplicationPageNames, PageViewModel>>(sp => name => name switch
        {
            ApplicationPageNames.AlgorithmConfigurator => sp.GetRequiredService<AlgorithmConfiguratorPageViewModel>(),
            ApplicationPageNames.HitlConfigurator  => sp.GetRequiredService<HitlConfiguratorPageViewModel>(),
            ApplicationPageNames.HitlScripting => sp.GetRequiredService<HitlScriptingPageViewModel>(),
            ApplicationPageNames.Results => sp.GetRequiredService<ResultsPageViewModel>(),
            ApplicationPageNames.AiConfigurator  => sp.GetRequiredService<AiConfiguratorPageViewModel>(),
            _ => throw new NotImplementedException()
        });

        services.AddSingleton<IAlgorithmConfigViewModelFactory, AlgorithmConfigViewModelFactory>();
        services.AddSingleton<Func<AlgorithmType, BaseAlgorithmConfigViewModel?>>(sp => name => name switch
        {
            AlgorithmType.Unknown => null,
            AlgorithmType.Genetic => sp.GetRequiredService<GaAlgorithmConfigViewModel>(),
            AlgorithmType.NSGA_II => sp.GetRequiredService<Nsga2AlgorithmConfigViewModel>(),
            AlgorithmType.ParticleSwarm => sp.GetRequiredService<PsoAlgorithmConfigViewModel>(),
            AlgorithmType.RAPGA => sp.GetRequiredService<RapGaAlgorithmConfigViewModel>(),
            _ => throw new NotImplementedException()
        });

        services.AddSingleton<IProblemResultViewModelFactory, ProblemResultViewModelFactory>();
        services.AddSingleton<Func<ProblemType, ProblemResultViewModel?>>(sp => (name) => name switch
        {
            ProblemType.Unknown => null,
            ProblemType.Function => sp.GetRequiredService<FunctionResultViewModel>(),
            ProblemType.Tsp => sp.GetRequiredService<TspResultViewModel>(),
            ProblemType.Qap => sp.GetRequiredService<QapResultViewModel>(),
            ProblemType.Jsp => sp.GetRequiredService<JspResultViewModel>(),
            _ => throw new NotImplementedException()
        });

        services.AddSingleton<IJobSelectorService, JobSelectorService>();

        // Script Manager
        services.AddSingleton<IScriptManager, ScriptManager>();

        // AI online services
        services.AddSingleton<IApiClient>(sp =>
        {
            var key = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY") ?? "";
            if (string.IsNullOrWhiteSpace(key))
            {
                sp.GetRequiredService<ILogger<OpenRouterClient>>()
                    .LogWarning("OPENROUTER_API_KEY environment variable is not set. AI features will be unavailable.");
            }
            return new OpenRouterClient(key);
        });
        services.AddSingleton<IScriptProvider, OpenRouterScriptProvider>();

        // Reflection Registry
        services.AddSingleton<ITypeRegistry, ReflectionRegistry>();
        services.AddSingleton<IDescriptorRegistry, DescriptorRegistry>();
    }

    public static void AddLoggerServices(this IServiceCollection services, LogLevel minimumLevel = LogLevel.Information)
    {
        services.AddLogging(builder =>
        {
            builder.ClearProviders();
            builder.SetMinimumLevel(minimumLevel);
            builder.AddConsole();
            builder.AddFilter("Microsoft", LogLevel.Warning);
            builder.AddFilter("System", LogLevel.Warning);
            builder.AddFilter("Synapse", minimumLevel);
        });
    }
}
