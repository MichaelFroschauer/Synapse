using Synapse.Problems.QAP;

namespace Synapse.HITL.Problems.QAP;

static class ConstraintHelpers
{
    public static int[] BuildInverse(QapSolution perm)
    {
        int n = perm.Length;
        int[] inverse = new int[n];
        for (int i = 0; i < n; ++i) inverse[i] = -1;
        for (int facility = 0; facility < n; ++facility)
        {
            int loc = perm.GetParameterWithType(facility);
            if (loc < 0 || loc >= n)
                throw new InvalidOperationException("Solution is not a valid permutation (location out of range).");
            inverse[loc] = facility;
        }
        return inverse;
    }
}
