using System.Text.RegularExpressions;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.JSP;

[ProblemParser(ProblemType = ProblemType.Jsp)]
public class JspProblemParser : IProblemParser
{
    public IProblem Parse(string inputFilePath)
    {
        if (string.IsNullOrWhiteSpace(inputFilePath)) throw new ArgumentNullException(nameof(inputFilePath));
        if (!File.Exists(inputFilePath)) throw new FileNotFoundException("Input file not found", inputFilePath);
        var lines = File.ReadAllLines(inputFilePath).Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();
        if (lines.Length < 3) throw new FormatException("Input file too short / malformed.");

        // First two lines are name/description (we ignore or store them if needed)
        var name = lines[0];
        var description = lines[1];

        // Third line: n m
        var nmTokens = Regex.Split(lines[2], @"\s+").Where(t => t.Length > 0).ToArray();
        if (nmTokens.Length < 2) throw new FormatException("Third line must contain two integers: n m.");
        if (!int.TryParse(nmTokens[0], out int n)) throw new FormatException("Could not parse number of jobs (n).");
        if (!int.TryParse(nmTokens[1], out int m)) throw new FormatException("Could not parse number of machines (m).");
        if (lines.Length < 3 + n) throw new FormatException($"Expected {n} job lines but file contains fewer.");
        var jobs = new Job[n];
        for (int jobIdx = 0; jobIdx < n; jobIdx++)
        {
            var line = lines[3 + jobIdx];
            var tokens = Regex.Split(line, @"\s+").Where(t => t.Length > 0).ToArray();
            if (tokens.Length != 2 * m)
                throw new FormatException(
                    $"Job line {jobIdx} must contain {2 * m} numbers (machine,time pairs) but has {tokens.Length}.");
            var operations = new Operation[m];
            for (int k = 0; k < m; k++)
            {
                if (!int.TryParse(tokens[2 * k], out int machine))
                    throw new FormatException($"Invalid machine id at job {jobIdx}, pair {k}.");
                if (!int.TryParse(tokens[2 * k + 1], out int time))
                    throw new FormatException($"Invalid processing time at job {jobIdx}, pair {k}.");
                operations[k] = new Operation(machine, time);
            }

            jobs[jobIdx] = new Job(operations);
        }

        return new JspProblem(jobs, m, name, description);
    }
}
