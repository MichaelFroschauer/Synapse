using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.QAP;

[ProblemParser(ProblemType = ProblemType.Qap)]
public class QapProblemParser : IProblemParser
{
    public IProblem Parse(string inputFilePath)
    {
        var input = File.ReadAllText(inputFilePath);
        
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Input should not be empty", nameof(input));
        
        var raw = input.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToArray();

        if (raw.Length < 1)
            throw new ArgumentException("Invalid format: no lines found");
        
        if (!int.TryParse(raw[0].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0], out int n))
            throw new FormatException("Could not parse dimensions (first line)");

        if (raw.Length < 1 + 2 * n)
            throw new FormatException($"Expected at least {1 + 2 * n} lines (n={n}), found {raw.Length}.");

        int idx = 1;
        int[,] D = new int[n, n];
        int[,] F = new int[n, n];

        for (int i = 0; i < n; i++, idx++)
        {
            var parts = raw[idx].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != n) throw new FormatException($"Line {idx} has {parts.Length} entries (expected {n}).");
            for (int j = 0; j < n; j++) D[i, j] = int.Parse(parts[j]);
        }

        for (int i = 0; i < n; i++, idx++)
        {
            var parts = raw[idx].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != n) throw new FormatException($"Line {idx} (2. matrix) has {parts.Length} entries (expected {n}).");
            for (int j = 0; j < n; j++) F[i, j] = int.Parse(parts[j]);
        }

        return new QapProblem(n, D, F);
    }
}
