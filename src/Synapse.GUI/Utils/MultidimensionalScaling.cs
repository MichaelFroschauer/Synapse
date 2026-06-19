using System;
using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;

namespace Synapse.GUI.Utils;

public static class MultidimensionalScaling
{
    /// <summary>
    /// Classical MDS: embed a distance matrix into k dimensions.
    /// Input: distMatrix (n x n) symmetric, diagonal zeros.
    /// Output: n x k array of coordinates.
    /// </summary>
    /// <param name="distMatrix">square distance matrix</param>
    /// <param name="k">target dimension (e.g. 2)</param>
    /// <param name="tolerance">eigenvalue tolerance for positivity</param>
    /// <returns>n x k coordinate array</returns>
    public static double[,] ClassicalMds(double[,] distMatrix, int k = 2, double tolerance = 1e-9)
    {
        if (distMatrix == null) throw new ArgumentNullException(nameof(distMatrix));
        int n = distMatrix.GetLength(0);
        if (distMatrix.GetLength(1) != n) throw new ArgumentException("distMatrix must be square");

        // Convert to MathNet matrix
        var D = DenseMatrix.OfArray(distMatrix);

        // 1) squared distances
        var D2 = D.PointwisePower(2.0);

        // 2) centering matrix J = I - (1/n) * 11^T
        var I = DenseMatrix.CreateIdentity(n);
        var ones = DenseMatrix.Create(n, n, 1.0);
        var J = I - (1.0 / n) * ones;

        // 3) double-centered inner product matrix B = -0.5 * J * D2 * J
        var B = (J * D2 * J).Multiply(-0.5);

        // 4) eigen decomposition of B
        // B should be symmetric; Evd() will return real eigenvalues (up to small imag parts)
        var evd = B.Evd();
        var eigenValues = evd.EigenValues;
        var eigenVectors = evd.EigenVectors;

        // Build array of (eigenvalueReal, eigenvector) and sort by eigenvalue desc
        var pairs = new (double val, Vector<double> vec)[n];
        for (int i = 0; i < n; i++)
        {
            double val = eigenValues[i].Real; // take real part
            var vec = eigenVectors.Column(i);
            pairs[i] = (val, vec);
        }
        Array.Sort(pairs, (a, b) => b.val.CompareTo(a.val)); // descending

        // 5) pick top k positive eigenvalues (or zero if below tolerance)
        int kUse = Math.Min(k, n);
        var coords = DenseMatrix.Create(n, kUse, 0.0);

        int taken = 0;
        for (int i = 0; i < pairs.Length && taken < kUse; i++)
        {
            double lambda = pairs[i].val;
            double lambdaPos = lambda > tolerance ? lambda : 0.0;
            double sqrtLambda = Math.Sqrt(lambdaPos); // if lambdaPos==0 -> coordinate zeros
            var colVec = pairs[i].vec.Multiply(sqrtLambda);
            coords.SetColumn(taken, colVec);
            taken++;
        }

        // If fewer than kUse eigenvalues were filled (shouldn't normally happen), remaining columns stay zero.
        var result = new double[n, kUse];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < kUse; j++)
                result[i, j] = coords[i, j];

        return result;
    }

    /// <summary>
    /// Overload für int[,] input.
    /// </summary>
    public static double[,] ClassicalMds(int[,] distMatrixInt, int k = 2, double tolerance = 1e-9)
    {
        if (distMatrixInt == null) throw new ArgumentNullException(nameof(distMatrixInt));
        int n = distMatrixInt.GetLength(0);
        if (distMatrixInt.GetLength(1) != n) throw new ArgumentException("distMatrixInt must be square");

        var dist = new double[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                dist[i, j] = distMatrixInt[i, j];

        return ClassicalMds(dist, k, tolerance);
    }

    /// <summary>
    /// Optional quick check: ensure symmetric and zero diagonal (with small tolerance).
    /// Throws if checks fail.
    /// </summary>
    public static void ValidateDistanceMatrix(double[,] distMatrix, double tol = 1e-6)
    {
        int n = distMatrix.GetLength(0);
        if (distMatrix.GetLength(1) != n) throw new ArgumentException("Dist matrix must be square.");
        for (int i = 0; i < n; i++)
        {
            if (Math.Abs(distMatrix[i, i]) > tol)
                throw new ArgumentException($"Diagonal at {i},{i} is not (close to) zero.");
            for (int j = i + 1; j < n; j++)
            {
                if (Math.Abs(distMatrix[i, j] - distMatrix[j, i]) > tol)
                    throw new ArgumentException($"Matrix is not symmetric at {i},{j} vs {j},{i}.");
                if (distMatrix[i, j] < -tol)
                    throw new ArgumentException($"Negative distance at {i},{j}.");
            }
        }
    }
}

/*
   double[,] D = new double[,]
   {
       { 0,27,85, 2, 1,15,11,35,11,20,21,61 },
       {27, 0,80,58,21,76,72,44,85,94,90,51 },
       {85,80, 0, 3,48,29,90,66,41,15,83,96 },
       { 2,58, 3, 0,74,45,65,40,54,83,14,71 },
       { 1,21,48,74, 0,77,36,53,37,26,87,76 },
       {15,76,29,45,77, 0,91,13,29,11,77,32 },
       {11,72,90,65,36,91, 0,87,67,94,79, 2 },
       {35,44,66,40,53,13,87, 0,10,99,56,70 },
       {11,85,41,54,37,29,67,10, 0,99,60, 4 },
       {20,94,15,83,26,11,94,99,99, 0,56, 2 },
       {21,90,83,14,87,77,79,56,60,56, 0,60 },
       {61,51,96,71,76,32, 2,70, 4, 2,60, 0 }
   };

   // optional: validate
   MultidimensionalScaling.ValidateDistanceMatrix(D);

   // compute 2D embedding
   var coords = MultidimensionalScaling.ClassicalMds(D, k: 2);

   // print
   for (int i = 0; i < coords.GetLength(0); i++)
   {
       Console.WriteLine($"L{i}: ({coords[i,0]:F4}, {coords[i,1]:F4})");
   }
*/
