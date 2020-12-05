using System.Collections.Generic;
using System.Linq;
using AIS.ClonalgPR.Models;

namespace AIS.ClonalgPR.Measures
{
    public class Hamming : IDistance
    {
        public double Calculate(char[] sequenceA, char[] sequenceB)
        {
            if (sequenceA.Length != sequenceB.Length) return 0;

            var matches = 0;
            for (int j = 0; j < sequenceA.Length; j++)
            {
                if (sequenceB[j] == sequenceA[j])
                    matches++;
            }
            return (double)matches / sequenceA.Length;
        }

        public double CalculateCloneRate(double affinity, int length)
        {
            return affinity;
        }

        public double CalculateMutationRate(double affinity, int length)
        {
            return 1 - affinity;
        }

        public bool IsBetterAffinity(double affinityAB, double affinityM)
        {
            return affinityAB > affinityM;
        }

        public List<Antibody> Order(List<Antibody> population, int numberHighAffinity)
        {
            return population.OrderByDescending(o => o.Affinity).Take(numberHighAffinity).ToList();
        }
    }
}
