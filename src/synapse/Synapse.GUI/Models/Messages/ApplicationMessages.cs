using System;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.GUI.Models.Messages;

public record AlgorithmConfigChangedMessage(IAlgorithmConfig Config);

public record ProblemSelectedMessage(IProblem Problem, IProblemInstance? ProblemInstance);

public record AlgorithmStartedMessage(Guid JobId);

public record AlgorithmConfigCloned();
