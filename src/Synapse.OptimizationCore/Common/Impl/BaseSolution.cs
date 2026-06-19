using Synapse.OptimizationCore.Interfaces;

namespace Synapse.OptimizationCore.Common.Impl;

public abstract class BaseSolution<T> : ISolution, IComparable<ISolution>
{
    private Parameter[] _parameters;
    
    public double? Fitness { get; set; }
    public int Length => _parameters.Length;
    
    protected BaseSolution(int length)
    { 
        _parameters = new Parameter[length];
    }
    
    protected BaseSolution(Parameter[] parameters)
    { 
        _parameters = parameters.Clone() as Parameter[] ?? [];
    }
    public abstract ISolution Create(T[] parameters);
    
    public Parameter[] GetParameters() => _parameters;
    public void SetParameters(Parameter[] parameters)
    {
        if (_parameters.Length != parameters.Length)
        {
            throw new ArgumentException($"Given parameters does not match expected parameters length (given: {parameters.Length}, expected: {_parameters.Length})");
        }
        _parameters = parameters.Clone() as Parameter[] ?? [];
    }

    public void SetParameters(T[] parameters)
    {
        _parameters = parameters.Select(c => c is null ? throw new ArgumentNullException(nameof(c)) : new Parameter(c)).ToArray();
    }

    public Parameter GetParameter(int index)
    {
        if (index < 0 || index >= _parameters.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        return _parameters[index];
    }

    public void SetParameter(int index, Parameter parameter)
    {
        if (index < 0 || index >= _parameters.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        _parameters[index] = parameter;
    }

    public T GetParameterWithType(int index)
    {
        if (index < 0 || index >= _parameters.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }
        
        if (_parameters[index].Value is T v)
        {
            return v;
        }

        throw new InvalidCastException($"Parameter is of type {_parameters[index].Value?.GetType().FullName ?? "null"}, expected {typeof(T).FullName}.");
    }
    
    public T[] GetParametersWithType()
    {
        T[] parameters = new T[_parameters.Length];
        for (int i = 0; i < _parameters.Length; ++i)
        {
            parameters[i] = GetParameterWithType(i);
        }
        return parameters;
    }

    public abstract ISolution CreateRandom();
    
    public ISolution Clone()
    {
        ISolution solution = CreateRandom();
        var parameters = GetParameters();
        solution.SetParameters(parameters);
        solution.Fitness = this.Fitness;
        return solution;
    }

    public abstract ISolutionSimilarity GetDefaultSolutionSimilarityClass();

    public int CompareTo(ISolution? other)
    {
        if (other == null)
          return -1;
        double? fitness1 = other.Fitness;
        double? nullable1 = this.Fitness;
        double? nullable2 = fitness1;
        if (nullable1.GetValueOrDefault() == nullable2.GetValueOrDefault() & nullable1.HasValue == nullable2.HasValue)
          return 0;
        double? fitness2 = this.Fitness;
        nullable1 = fitness1;
        return !(fitness2.GetValueOrDefault() > nullable1.GetValueOrDefault() & fitness2.HasValue & nullable1.HasValue) ? -1 : 1;
    }
}
