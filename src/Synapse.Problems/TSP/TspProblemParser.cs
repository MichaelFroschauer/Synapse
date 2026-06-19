using System.Globalization;
using System.Text.RegularExpressions;
using Synapse.OptimizationCore.Attributes;
using Synapse.OptimizationCore.Common;
using Synapse.OptimizationCore.Interfaces;

namespace Synapse.Problems.TSP;

public static class QualityParser {
    public static double GetBestQuality(string instanceName)
    {
        // TODO remove path
        string filePath =
            "/home/michael/gitclones/Masterarbeit/master-thesis-hitl/src/Synapse/Problems/Synapse.Problems/TSP/Data/tsp.qual";
        if (!File.Exists(filePath)) return 0.0;
        
        using (var rdr = new StreamReader(filePath))
        {
            string? line;
            while ((line = rdr.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;
                
                var kvParts = line.Split(';', 2, StringSplitOptions.RemoveEmptyEntries);
                if (kvParts.Length == 2)
                {
                    var key  = kvParts[0].Trim().ToUpperInvariant();
                    if (key == instanceName.Trim().ToUpperInvariant())
                    {
                        var value = kvParts[1];
                        return Convert.ToDouble(value, CultureInfo.InvariantCulture);    
                    }    
                }
            }
        }
        return 0.0;
    }
}


[ProblemParser(ProblemType = ProblemType.Tsp)]
public class TspProblemParser : IProblemParser
{
    public IProblem Parse(string inputFilePath)
    {
        if (string.IsNullOrWhiteSpace(inputFilePath))
            throw new ArgumentException("inputFilePath is null or empty.", nameof(inputFilePath));
        if (!File.Exists(inputFilePath))
            throw new FileNotFoundException("TSP input file not found.", inputFilePath);

        var coords = new List<(double X, double Y)>();
        int? declaredDimension = null;
        string? edgeWeightType = null;
        bool inNodeSection = false;
        string problemInstanceName = "";

        // Regex to split on whitespace
        var wsSplit = new Regex(@"\s+");

        using (var rdr = new StreamReader(inputFilePath))
        {
            string? line;
            while ((line = rdr.ReadLine()) != null)
            {
                line = line.Trim();
                if (string.IsNullOrEmpty(line))
                    continue;

                // End condition
                if (line.Equals("EOF", StringComparison.OrdinalIgnoreCase))
                    break;

                if (!inNodeSection)
                {
                    // Header parsing
                    if (line.StartsWith("NODE_COORD_SECTION", StringComparison.OrdinalIgnoreCase))
                    {
                        inNodeSection = true;
                        continue;
                    }

                    // parse simple "KEY: VALUE" header lines (some files also use "KEY VALUE")
                    var kvParts = line.Split(new[] { ':', ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);
                    if (kvParts.Length >= 2)
                    {
                        var key = kvParts[0].Trim().ToUpperInvariant();
                        var val = line.Substring(line.IndexOf(':') >= 0 ? line.IndexOf(':') + 1 : kvParts[0].Length).Trim();

                        if (key == "DIMENSION")
                        {
                            if (int.TryParse(val, NumberStyles.Integer, CultureInfo.InvariantCulture, out var dim))
                                declaredDimension = dim;
                        }
                        else if (key == "EDGE_WEIGHT_TYPE")
                        {
                            edgeWeightType = val.ToUpperInvariant();
                        }
                        else if (key == "NAME")
                        {
                            problemInstanceName = val;
                        }
                    }
                }
                else
                {
                    // NODE_COORD_SECTION lines
                    // expected forms:
                    // "1 565.0 575.0"
                    var parts = wsSplit.Split(line).Where(s => !string.IsNullOrWhiteSpace(s)).ToArray();
                    // index x y
                    if (parts.Length == 3 &&
                        double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double x) &&
                        double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out double y))
                    {
                        coords.Add((x, y));
                    }
                    else
                    {
                        throw new InvalidDataException($"Problem file contains unexpected format: {line}");
                    }
                }
            }
        }

        // Validate dimension if declared
        if (declaredDimension.HasValue && declaredDimension.Value != coords.Count)
        {
            throw new InvalidDataException($"Declared DIMENSION = {declaredDimension.Value} but found {coords.Count} coordinates.");
        }

        if (coords.Count == 0)
        {
            throw new InvalidDataException("No coordinates found in TSP file.");   
        }
        
        bool roundEuc = !string.IsNullOrEmpty(edgeWeightType) && edgeWeightType.Contains("EUC");
        int n = coords.Count;
        var distanceMatrix = new double[n, n];
        
        for (int i = 0; i < n; i++)
        {
            distanceMatrix[i, i] = 0.0;
            for (int j = i + 1; j < n; j++)
            {
                var dx = coords[i].X - coords[j].X;
                var dy = coords[i].Y - coords[j].Y;
                double d = Math.Sqrt(dx * dx + dy * dy);
                if (roundEuc)
                    d = Math.Round(d);
                distanceMatrix[i, j] = d;
                distanceMatrix[j, i] = d;
            }
        }

        return new TspProblem(distanceMatrix, coords)
        {
            BestKnownFitness =  QualityParser.GetBestQuality(problemInstanceName),
            InstanceName = problemInstanceName,
            XMaxCoordinate = coords.Max(c => c.X),
            YMaxCoordinate = coords.Max(c => c.Y)
        };
    }
}
