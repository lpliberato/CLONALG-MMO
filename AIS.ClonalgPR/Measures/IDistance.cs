using AIS.ClonalgPR.Models;
using System.Collections.Generic;

namespace AIS.ClonalgPR.Measures
{
    public interface IDistance
    {
        double Calculate(char[] sequenceA = null, char[] sequenceB = null);
        IEnumerable<Antibody> Order(List<Antibody> population);
        double CalculateCloneRate(double affinity, int length);
        double CalculateMutationRate(double affinity, int length);
        bool IsBetterAffinity(double affinityAB, double affinityM);
        int SequenceSize();
    }
}
