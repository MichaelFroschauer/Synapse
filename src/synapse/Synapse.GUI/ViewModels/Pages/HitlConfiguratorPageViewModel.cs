using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using ActiproSoftware.Extensions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Synapse.GUI.Models;
using Synapse.GUI.Services;
using Synapse.GUI.ViewModels.Controls;
using Synapse.GUI.ViewModels.Controls.AlgorithmConfig;
using Synapse.GUI.ViewModels.Controls.SolutionInput;
using Synapse.JobManagement;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.HITL;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Reflection;
using Synapse.Reflection.Models;

namespace Synapse.GUI.ViewModels.Pages;

public class UserSettableSolutionEditor : ObservableObject
{
    public int UserSettableLength { get; set; } = 0;
    public required UserSettableClass UserSettableClass { get; set; }
    public double SolutionEditProbability { get; set; } = 80.0;
    public int SolutionEditExecuteForNrOfIterations { get; set; } = 1;
    public required SolutionInputViewModel? SolutionInputViewModel { get; set; }
}

public class ConstraintDisplayItem : ObservableObject
{
    public required IConstraint Constraint { get; init; }

    public string HeaderText => BuildHeader(Constraint);

    public string BodyText => BuildBody(Constraint);

    private static string BuildHeader(IConstraint constraint)
    {
        var shortText = string.IsNullOrWhiteSpace(constraint.Description)
            ? constraint.GetType().Name
            : constraint.Description.Trim();

        var handling = constraint.RepairSolution ? "On violation: repair" : "On violation: discard";
        var parameterText = DisplayFormattingHelpers.BuildObjectParameterSummary(constraint);

        if (string.IsNullOrWhiteSpace(parameterText))
            return $"{shortText} | {handling}";

        return $"{shortText} | {handling} | {parameterText}";
    }

    private static string BuildBody(IConstraint constraint)
    {
        var sb = new StringBuilder();
        sb.AppendLine(constraint.GetType().Name);
        sb.AppendLine(constraint.TextVisualization);
        sb.AppendLine();
        sb.AppendLine($"Violation handling: {(constraint.RepairSolution ? "Repair candidate" : "Discard candidate")}");

        var parameterText = DisplayFormattingHelpers.BuildObjectParameterSummary(constraint, useMultiline: true);
        if (!string.IsNullOrWhiteSpace(parameterText))
        {
            sb.AppendLine();
            sb.AppendLine("Parameters:");
            sb.AppendLine(parameterText);
        }

        return sb.ToString().TrimEnd();
    }
}

public class ManualEditDisplayItem : ObservableObject
{
    public required SolutionEditor ManualEdit { get; init; }

    public string HeaderText => BuildHeader(ManualEdit);

    public string BodyText => BuildBody(ManualEdit);

    private static string BuildHeader(SolutionEditor edit)
    {
        var name = DisplayFormattingHelpers.GetDisplayName(edit.EditApplier.GetType());
        var probabilityPercent = Math.Round(edit.EditProbability * 100, 2);
        var iterBudget = edit.ExecuteForNrOfIterations <= 0 ? "infinite" : edit.ExecuteForNrOfIterations.ToString();
        var applierParams = DisplayFormattingHelpers.BuildObjectParameterSummary(edit.EditApplier);

        var parts = new List<string>
        {
            name,
            $"p={probabilityPercent}%",
            $"iters={iterBudget}"
        };

        if (!string.IsNullOrWhiteSpace(applierParams))
            parts.Add(applierParams);

        return string.Join(" | ", parts);
    }

    private static string BuildBody(SolutionEditor edit)
    {
        var sb = new StringBuilder();
        var applierType = edit.EditApplier.GetType();
        sb.AppendLine(DisplayFormattingHelpers.GetDisplayName(applierType));

        var displayInfo = applierType.GetCustomAttribute<DisplayInfoAttribute>();
        if (!string.IsNullOrWhiteSpace(displayInfo?.Description))
            sb.AppendLine(displayInfo.Description);

        sb.AppendLine();
        sb.AppendLine($"Probability: {Math.Round(edit.EditProbability * 100, 2)}%");
        sb.AppendLine($"Iteration budget: {(edit.ExecuteForNrOfIterations <= 0 ? "infinite" : edit.ExecuteForNrOfIterations)}");

        var applierParams = DisplayFormattingHelpers.BuildObjectParameterSummary(edit.EditApplier, useMultiline: true);
        if (!string.IsNullOrWhiteSpace(applierParams))
        {
            sb.AppendLine("Applier parameters:");
            sb.AppendLine(applierParams);
        }

        var manualParams = edit.SolutionPrototype.GetParameters();
        var preview = string.Join(", ", manualParams.Take(12).Select(p => p.ToString()));
        if (manualParams.Length > 12) preview += ", ...";
        sb.AppendLine($"Manual solution preview: [{preview}]");

        return sb.ToString().TrimEnd();
    }
}

public class AvailableConstraintDisplayItem : ObservableObject
{
    private readonly UserSettableClass _constraintDefinition = null!;
    public required UserSettableClass ConstraintDefinition
    {
        get => _constraintDefinition;
        init
        {
            _constraintDefinition = value;
            foreach (var p in _constraintDefinition.UserSettableProperties)
            {
                p.PropertyChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(HeaderText));
                    OnPropertyChanged(nameof(BodyText));
                };
            }
        }
    }
    public string HeaderText => ConstraintDefinition.DisplayName;
    public string BodyText => ConstraintDefinition.Description ?? string.Empty;
}

public class AvailableManualEditDisplayItem : ObservableObject
{
    private readonly UserSettableSolutionEditor _editorDefinition = null!;
    public required UserSettableSolutionEditor EditorDefinition
    {
        get => _editorDefinition;
        init
        {
            _editorDefinition = value;
            foreach (var p in _editorDefinition.UserSettableClass.UserSettableProperties)
            {
                p.PropertyChanged += (_, _) =>
                {
                    OnPropertyChanged(nameof(HeaderText));
                    OnPropertyChanged(nameof(BodyText));
                };
            }
        }
    }
    public string HeaderText => EditorDefinition.UserSettableClass.DisplayName;
    public string BodyText => EditorDefinition.UserSettableClass.Description ?? string.Empty;
}

public class PreferenceDisplayItem : ObservableObject
{
    public required SolutionPreference Preference { get; init; }

    public string HeaderText => BuildHeader(Preference);
    public string BodyText => BuildBody(Preference);

    private static string BuildHeader(SolutionPreference preference)
    {
        var mode = preference.HasSolutionSimilarity ? "Similarity" : "Partial Match";
        return $"{preference.Name} | {mode} | weight={preference.Weight:F2}";
    }

    private static string BuildBody(SolutionPreference preference)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Name: {preference.Name}");
        sb.AppendLine($"Weight: {preference.Weight:F2}");
        sb.AppendLine($"Mode: {(preference.HasSolutionSimilarity ? "Similarity to reference solution" : "Partial exact matcher")}");

        if (preference.HasSolutionSimilarity)
        {
            sb.AppendLine($"Similarity evaluator: {preference.SimilarityEvaluator?.GetType().Name ?? "-"}");
            var preview = preference.ReferenceSolution?.ToString() ?? "-";
            if (preview.Length > 240) preview = preview[..240] + "...";
            sb.AppendLine($"Reference solution: {preview}");
        }

        return sb.ToString().TrimEnd();
    }
}

internal static class DisplayFormattingHelpers
{
    public static string GetDisplayName(Type type)
    {
        var display = type.GetCustomAttribute<DisplayInfoAttribute>();
        return string.IsNullOrWhiteSpace(display?.Name) ? type.Name : display.Name;
    }

    public static string BuildSettableSummary(IEnumerable<UserSettableProperty> properties, bool useMultiline = false)
    {
        var entries = properties.Select(p => BuildKeyValue(p.DisplayName, p.Value)).ToList();
        return JoinEntries(entries, useMultiline);
    }

    public static string BuildObjectParameterSummary(object obj, bool useMultiline = false)
    {
        var entries = new List<string>();
        var type = obj.GetType();

        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0) continue;
            if (prop.Name is nameof(IConstraint.Description) or nameof(IConstraint.TextVisualization) or nameof(IConstraint.RepairSolution)) continue;
            if (IsTechnicalName(prop.Name)) continue;
            if (IsTechnicalType(prop.PropertyType)) continue;

            object? value;
            try { value = prop.GetValue(obj); }
            catch { continue; }

            if (value is null) continue;
            if (IsDefaultValue(value)) continue;
            if (!IsDisplayableValue(value)) continue;
            entries.Add(BuildKeyValue(prop.Name, value));
        }

        foreach (var field in type.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
        {
            if (field.Name.StartsWith("<")) continue;
            if (IsTechnicalName(field.Name)) continue;
            if (IsTechnicalType(field.FieldType)) continue;

            var value = field.GetValue(obj);
            if (value is null) continue;
            if (IsDefaultValue(value)) continue;
            if (!IsDisplayableValue(value)) continue;
            entries.Add(BuildKeyValue(field.Name.TrimStart('_'), value));
        }

        return JoinEntries(entries.Distinct().ToList(), useMultiline);
    }

    private static bool IsTechnicalName(string name)
    {
        var normalized = name.TrimStart('_');
        return normalized.Equals("rnd", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("rng", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("problem", StringComparison.OrdinalIgnoreCase)
               || normalized.Equals("d", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("matrix", StringComparison.OrdinalIgnoreCase)
               || normalized.Contains("inverse", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTechnicalType(Type t)
    {
        if (t == typeof(Type)) return true;
        if (typeof(IProblem).IsAssignableFrom(t)) return true;
        if (typeof(OptimizationCore.Random.IRandom).IsAssignableFrom(t)) return true;
        if (t.IsArray && t.GetArrayRank() > 1) return true;
        return false;
    }

    private static bool IsDisplayableValue(object value)
    {
        if (value is string) return true;
        if (value is IEnumerable enumerable)
        {
            var preview = enumerable.Cast<object?>().Take(12).ToList();
            return preview.All(v => v is null || v is string || v.GetType().IsPrimitive || v is ValueType);
        }

        var type = value.GetType();
        return type.IsPrimitive || value is ValueType;
    }

    private static bool IsDefaultValue(object value)
    {
        return value switch
        {
            bool b when !b => true,
            int i when i == 0 => true,
            long l when l == 0 => true,
            double d when Math.Abs(d) < double.Epsilon => true,
            float f when Math.Abs(f) < float.Epsilon => true,
            string s when string.IsNullOrWhiteSpace(s) => true,
            _ => false
        };
    }

    private static string BuildKeyValue(string key, object? value)
    {
        var valueText = value switch
        {
            null => "-",
            IEnumerable enumerable when value is not string => string.Join(", ", enumerable.Cast<object?>().Select(v => v?.ToString() ?? "null")),
            _ => value.ToString() ?? "-"
        };
        return $"{key}={valueText}";
    }

    private static string JoinEntries(IReadOnlyList<string> entries, bool useMultiline)
    {
        if (entries.Count == 0) return string.Empty;
        return useMultiline ? string.Join(Environment.NewLine, entries.Select(e => $"- {e}")) : string.Join(", ", entries);
    }
}

public partial class HitlConfiguratorPageViewModel : PageViewModel
{
    private readonly IJobSelectorService _jobSelector;
    private readonly IJobManager _jobManager;
    private readonly ITypeRegistry _typeRegistry;
    private readonly IAlgorithmConfigViewModelService _configViewModelService;
    private readonly IProblemResultViewModelService _problemResultViewModelService;

    public SolutionPreferenceSelectorViewModel SolutionPreferenceVm { get; }
    public InteractionTriggersViewModel InteractionTriggersVm { get; }
    public ObservableCollection<ConstraintDisplayItem> Constraints { get; } = new();
    public ObservableCollection<ManualEditDisplayItem> ManualEdits { get; } = new();
    public ObservableCollection<PreferenceDisplayItem> Preferences { get; } = new();

    public ObservableCollection<string> PreferenceModes { get; } = new()
    {
        "Similarity (whole solution)",
        "Partial match (active positions only)"
    };

    [ObservableProperty] private string _selectedPreferenceMode = "Similarity (whole solution)";
    [ObservableProperty] private string _newPreferenceName = string.Empty;
    [ObservableProperty] private double _newPreferenceWeight = 0.2;
    [ObservableProperty] private string _preferenceInputError = string.Empty;
    [ObservableProperty] private PreferenceSolutionInputViewModel? _preferenceInputViewModel;
    
    [ObservableProperty] private BaseAlgorithmConfigViewModel? _currentAlgorithmParameters;
    
    public ObservableCollection<AvailableConstraintDisplayItem> AvailableConstraints { get; } = new();
    public ObservableCollection<AvailableManualEditDisplayItem> AvailableManualEdits { get; } = new();

    [ObservableProperty] private SolutionInputViewModel? _solutionInputViewModel;
    
    private SolutionInputFactory _solutionInputFactory = new(); 

    public HitlConfiguratorPageViewModel(
        IAlgorithmConfigViewModelService configViewModelService,
        IProblemResultViewModelService problemResultViewModelService,
        IJobSelectorService jobSelector,
        IJobManager jobManager,
        ITypeRegistry typeRegistry,
        SolutionPreferenceSelectorViewModel solutionPreferenceVm,
        InteractionTriggersViewModel interactionTriggersVm)
    {
        PageName = ApplicationPageNames.HitlConfigurator;
        
        _jobSelector = jobSelector;
        _jobManager = jobManager;
        _typeRegistry = typeRegistry;
        _configViewModelService = configViewModelService;
        _problemResultViewModelService = problemResultViewModelService;

        SolutionPreferenceVm = solutionPreferenceVm;
        InteractionTriggersVm = interactionTriggersVm;
        
        _jobSelector.SelectedJobChanged += OnSelectedJobChanged;
        OnSelectedJobChanged(null, null);
    }

    private void OnSelectedJobChanged(object? sender, SelectedJobChangedEventArgs? e)
    {
        CurrentAlgorithmParameters = _configViewModelService.CurrentAlgorithmConfigViewModel;

        Constraints.Clear();
        ManualEdits.Clear();
        Preferences.Clear();
        AvailableConstraints.Clear();
        AvailableManualEdits.Clear();
        
        ISolution? solution = _jobSelector.SelectedJobInfo?.Problem.CreateRandomSolution(); 
        Type? t = solution?.GetType();
        if (t is not null)
        {
            PreferenceInputViewModel = new PreferenceSolutionInputViewModel(solution!);
            PreferenceInputError = string.Empty;

            var availableConstraints = _typeRegistry.GetAssignableTypes<IConstraint>()
                .BuildUserSettableClasses()
                .ApplicableToSolution(t)
                .Select(c => new AvailableConstraintDisplayItem { ConstraintDefinition = c });
            AvailableConstraints.AddRange(availableConstraints);

            var availableManualEdits = _typeRegistry.GetAssignableTypes<IManualEditApplier>()
                .BuildUserSettableClasses()
                .ApplicableToSolution(t)
                .Select(e => new AvailableManualEditDisplayItem
                {
                    EditorDefinition = new UserSettableSolutionEditor()
                    {
                        UserSettableLength = solution!.Length,
                        UserSettableClass = e,
                        SolutionInputViewModel = _solutionInputFactory.GetViewModel(solution)
                    }
                });
            AvailableManualEdits.AddRange(availableManualEdits);
        }

        var hitlCtrl = GetCurrentHitlController();
        if (hitlCtrl is not null) {
            var constraints = hitlCtrl.GetConstraints().Select(c => new ConstraintDisplayItem { Constraint = c }).ToList();
            Constraints.AddRange(constraints);

            var manualEdits = hitlCtrl.GetManualEdits().Select(m => new ManualEditDisplayItem { ManualEdit = m }).ToList();
            ManualEdits.AddRange(manualEdits);

            var preferences = hitlCtrl.GetPreferences().Select(p => new PreferenceDisplayItem { Preference = p }).ToList();
            Preferences.AddRange(preferences);
        }

        if (_jobSelector.SelectedJobInfo is not null)
        {
            _problemResultViewModelService.SetProblemType(_jobSelector.SelectedJobInfo.Problem.ProblemType);
        }
    }

    [RelayCommand]
    private void AddConstraint(AvailableConstraintDisplayItem displayItem)
    {
        var userSettableConstraint = displayItem.ConstraintDefinition;
        SetProblemIfNecessary(userSettableConstraint);
        if (userSettableConstraint.TryGetInstance<IConstraint>(out var constraint, out var error))
        {
            var hitlCtrl = GetCurrentHitlController();
            if (constraint is not null)
            {
                hitlCtrl?.AddConstraint(constraint);
                Constraints.Add(new ConstraintDisplayItem { Constraint = constraint });
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[HITL] Failed to create constraint: {error}");
        }
    }

    [RelayCommand]
    private void AddManualEdit(AvailableManualEditDisplayItem displayItem)
    {
        var userSettableSolutionEditor = displayItem.EditorDefinition;
        SetProblemIfNecessary(userSettableSolutionEditor.UserSettableClass);
        if (userSettableSolutionEditor.UserSettableClass.TryGetInstance<IManualEditApplier>(out var manualEdit, out var error) &&
            userSettableSolutionEditor.SolutionInputViewModel is not null)
        {
            var hitlCtrl = GetCurrentHitlController();
            if (manualEdit is not null && hitlCtrl is not null)
            {
                var id = hitlCtrl.AddManualEdit(userSettableSolutionEditor.SolutionInputViewModel.Solution,
                    applier: manualEdit,
                    probability: userSettableSolutionEditor.SolutionEditProbability / 100.0,
                    executeForNrOfIterations: userSettableSolutionEditor.SolutionEditExecuteForNrOfIterations);
                ManualEdits.Add(new ManualEditDisplayItem { ManualEdit = hitlCtrl.GetManualEdit(id) });
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"[HITL] Failed to create manual edit: {error}");
        }
    }
    
    [RelayCommand]
    private void DeleteConstraint(ConstraintDisplayItem displayItem)
    {
        var hitlCtrl = GetCurrentHitlController();
        hitlCtrl?.RemoveConstraint(displayItem.Constraint);
        Constraints.Remove(displayItem);
    }

    [RelayCommand]
    private void DeleteManualEdit(ManualEditDisplayItem displayItem)
    {
        var hitlCtrl = GetCurrentHitlController();
        if (displayItem.ManualEdit.Name != null) hitlCtrl?.ClearManualEdit(displayItem.ManualEdit.Name);
        ManualEdits.Remove(displayItem);
    }
    
    [RelayCommand]
    private void DeletePreference(PreferenceDisplayItem displayItem)
    {
        var hitlCtrl = GetCurrentHitlController();
        hitlCtrl?.ClearPreference(displayItem.Preference.Name);
        Preferences.Remove(displayItem);
    }

    [RelayCommand]
    private void AddPreference()
    {
        PreferenceInputError = string.Empty;

        var hitlCtrl = GetCurrentHitlController();
        var inputVm = PreferenceInputViewModel;
        if (hitlCtrl is null || inputVm is null)
        {
            PreferenceInputError = "No active job/problem context available.";
            return;
        }

        if (double.IsNaN(NewPreferenceWeight) || double.IsInfinity(NewPreferenceWeight))
        {
            PreferenceInputError = "Please provide a valid preference weight.";
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewPreferenceName)
            ? $"GuiPreference-{Guid.NewGuid():N}"[..24]
            : NewPreferenceName.Trim();

        if (SelectedPreferenceMode.StartsWith("Similarity", StringComparison.OrdinalIgnoreCase))
        {
            if (!inputVm.TryBuildReferenceSolution(out var referenceSolution, out var error))
            {
                PreferenceInputError = error;
                return;
            }

            ISolutionSimilarity similarity;
            if (SolutionPreferenceVm.SelectedSolutionSimilarity is not null &&
                SolutionPreferenceVm.SelectedSolutionSimilarity.TryGetInstance<ISolutionSimilarity>(out var selected, out _) &&
                selected is not null)
            {
                similarity = selected;
            }
            else
            {
                similarity = hitlCtrl.GetSolutionSimilarity(referenceSolution);
            }

            hitlCtrl.AddSolutionPreference(similarity, referenceSolution, NewPreferenceWeight, name);
        }
        else
        {
            if (!inputVm.TryBuildPartialMatcher(out var matcher, out var error))
            {
                PreferenceInputError = error;
                return;
            }

            hitlCtrl.AddSolutionPreference(matcher, NewPreferenceWeight, name);
        }

        var added = hitlCtrl.GetPreferences().FirstOrDefault(p => p.Name == name);
        if (added is not null)
        {
            var existing = Preferences.FirstOrDefault(p => p.Preference.Name == name);
            if (existing is not null)
                Preferences.Remove(existing);
            Preferences.Add(new PreferenceDisplayItem { Preference = added });
        }

        NewPreferenceName = string.Empty;
    }
    
    private IHitlController? GetCurrentHitlController()
    {
        var jobInfo = _jobManager.GetJobInfo(_jobSelector.SelectedJobId);
        return jobInfo?.Config.HitlController;
    }

    private void SetProblemIfNecessary(UserSettableClass userSettableClass)
    {
        foreach (var p in userSettableClass.NonUserSettableProperties)
        {
            if (_jobSelector.SelectedJobInfo != null && _jobSelector.SelectedJobInfo.Problem.GetType().IsAssignableTo(p.Type))
            {
                p.Value = _jobSelector.SelectedJobInfo.Problem;
            }
        }
    }

    protected override void DisposeManaged()
    {
        _jobSelector.SelectedJobChanged -= OnSelectedJobChanged;
        base.DisposeManaged();
    }
}
