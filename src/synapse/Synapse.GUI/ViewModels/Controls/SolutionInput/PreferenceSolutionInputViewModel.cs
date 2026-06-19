using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.ViewModels.Controls.SolutionInput;

public class PreferenceParameterEntry : ObservableObject
{
    private bool _isActive = true;
    private string _valueText = string.Empty;

    public bool IsActive
    {
        get => _isActive;
        set => SetProperty(ref _isActive, value);
    }

    public string ValueText
    {
        get => _valueText;
        set => SetProperty(ref _valueText, value);
    }

    public required int Index { get; init; }
    public required Type ParameterType { get; init; }

    public string TypeLabel => ParameterType.Name;
}

public partial class PreferenceSolutionInputViewModel : ViewModelBase
{
    private readonly ISolution _solutionTemplate;

    public ObservableCollection<PreferenceParameterEntry> Entries { get; } = new();

    public string ProblemSolutionType => _solutionTemplate.GetType().Name;

    public PreferenceSolutionInputViewModel(ISolution template)
    {
        _solutionTemplate = template.Clone();

        var parameters = _solutionTemplate.GetParameters();
        for (var i = 0; i < parameters.Length; i++)
        {
            var value = parameters[i].Value;
            Entries.Add(new PreferenceParameterEntry
            {
                Index = i,
                ParameterType = value.GetType(),
                ValueText = ConvertToString(value)
            });
        }
    }

    [RelayCommand]
    private void SelectAllEntries()
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            if (!Entries[i].IsActive)
                Entries[i].IsActive = true;
        }
    }

    [RelayCommand]
    private void UnselectAllEntries()
    {
        for (var i = 0; i < Entries.Count; i++)
        {
            if (Entries[i].IsActive)
                Entries[i].IsActive = false;
        }
    }

    public bool TryBuildReferenceSolution(out ISolution referenceSolution, out string error)
    {
        var cloned = _solutionTemplate.Clone();
        var newParameters = new Parameter[Entries.Count];

        for (var i = 0; i < Entries.Count; i++)
        {
            var entry = Entries[i];
            if (!TryParseValue(entry, out var parsed, out error))
            {
                referenceSolution = cloned;
                return false;
            }

            newParameters[i] = new Parameter(parsed!);
        }

        cloned.SetParameters(newParameters);
        referenceSolution = cloned;
        error = string.Empty;
        return true;
    }

    public bool TryBuildPartialMatcher(out Func<ISolution, bool> matcher, out string error)
    {
        var activeEntries = Entries.Where(e => e.IsActive).ToList();
        if (activeEntries.Count == 0)
        {
            matcher = _ => false;
            error = "Please activate at least one position for partial preference matching.";
            return false;
        }

        var targetValues = new Dictionary<int, object>();
        foreach (var entry in activeEntries)
        {
            if (!TryParseValue(entry, out var parsed, out error))
            {
                matcher = _ => false;
                return false;
            }

            targetValues[entry.Index] = parsed!;
        }

        matcher = candidate =>
        {
            if (candidate.Length != Entries.Count) return false;
            foreach (var kv in targetValues)
            {
                var candidateValue = candidate.GetParameter(kv.Key).Value;
                if (!ValuesEqual(candidateValue, kv.Value))
                    return false;
            }

            return true;
        };

        error = string.Empty;
        return true;
    }

    private static bool TryParseValue(PreferenceParameterEntry entry, out object? parsed, out string error)
    {
        var text = entry.ValueText.Trim();
        var type = entry.ParameterType;

        try
        {
            if (type == typeof(int))
            {
                if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                {
                    parsed = null;
                    error = $"Position {entry.Index}: expected integer.";
                    return false;
                }

                parsed = i;
                error = string.Empty;
                return true;
            }

            if (type == typeof(long))
            {
                if (!long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                {
                    parsed = null;
                    error = $"Position {entry.Index}: expected long integer.";
                    return false;
                }

                parsed = l;
                error = string.Empty;
                return true;
            }

            if (type == typeof(double))
            {
                if (!double.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var d))
                {
                    parsed = null;
                    error = $"Position {entry.Index}: expected floating-point value (use '.' as decimal separator).";
                    return false;
                }

                parsed = d;
                error = string.Empty;
                return true;
            }

            if (type == typeof(float))
            {
                if (!float.TryParse(text, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var f))
                {
                    parsed = null;
                    error = $"Position {entry.Index}: expected floating-point value (use '.' as decimal separator).";
                    return false;
                }

                parsed = f;
                error = string.Empty;
                return true;
            }

            if (type == typeof(short))
            {
                if (!short.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var s))
                {
                    parsed = null;
                    error = $"Position {entry.Index}: expected short integer.";
                    return false;
                }

                parsed = s;
                error = string.Empty;
                return true;
            }

            parsed = text;
            error = string.Empty;
            return true;
        }
        catch
        {
            parsed = null;
            error = $"Position {entry.Index}: could not parse value '{entry.ValueText}'.";
            return false;
        }
    }

    private static bool ValuesEqual(object? candidateValue, object expectedValue)
    {
        if (candidateValue is null) return false;

        if (candidateValue is double cd && expectedValue is double ed)
            return Math.Abs(cd - ed) < 1e-9;

        if (candidateValue is float cf && expectedValue is float ef)
            return Math.Abs(cf - ef) < 1e-6f;

        return Equals(candidateValue, expectedValue);
    }

    private static string ConvertToString(object? value)
    {
        return value switch
        {
            null => string.Empty,
            double d => d.ToString("G17", CultureInfo.InvariantCulture),
            float f => f.ToString("G9", CultureInfo.InvariantCulture),
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };
    }
}
