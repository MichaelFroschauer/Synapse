using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;
using Synapse.Problems;

namespace Synapse.Reflection.Descriptors;

public class ProblemInstanceDescriptor : BaseDescriptor
{
    public ProblemType ProblemTypeEnum { get; init; }
    
    private readonly Type? _problemType;
    private bool IsFileInstance { get; init; }
    private bool IsCodeInstance { get; init; }
    private IProblemParser? ProblemParser { get; init; }
    private string? ProblemInstanceFilePath { get; init; }
    
    
    public ProblemInstanceDescriptor(Type problemType, Type type, ProblemInstanceAttribute attr) : base(type, attr)
    {
        _problemType = problemType;
        ProblemTypeEnum = attr?.ProblemType ?? ProblemType.Unknown;
        IsCodeInstance = true;
    }

    public ProblemInstanceDescriptor(IProblemParser parser, string name, string filePath, ProblemType problemTypeEnum)
        : base(null!, new DisplayInfoAttribute(name))
    {
        ProblemParser = parser;
        ProblemTypeEnum = problemTypeEnum;
        ProblemInstanceFilePath = filePath;
        IsFileInstance = true;
    }

    public IProblem CreateProblem()
    {
        if (IsCodeInstance)
        {
            if (_problemType is null) throw new ArgumentNullException(nameof(_problemType)); 
            var problemInstance = SettableClass?.GetInstance();
            var problem = Activator.CreateInstance(_problemType,  problemInstance) as IProblem;
            if (problem is null)
            {
                throw new InvalidOperationException($"Cannot create instance of {_problemType.FullName}.");
            }
            
            return problem;
        }
        
        if (IsFileInstance)
        {
            if (ProblemInstanceFilePath is null || !File.Exists(ProblemInstanceFilePath) || ProblemParser is null)
            {
                throw new InvalidOperationException($"Cannot create instance of {ProblemInstanceFilePath}.");
            }

            return ProblemParser.Parse(ProblemInstanceFilePath);
        }
        
        throw new InvalidOperationException($"Cannot create instance of {Enum.GetName(ProblemTypeEnum)}.");
    }
}
